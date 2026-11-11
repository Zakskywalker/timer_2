using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer2.Core.Storage;

namespace Timer2.App.Updates;

internal static class AppUpdateService
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/Zakskywalker/timer_2/releases/latest";
    private const string UpdateAssetName = "TimeRecorder_2.0_App.exe";

    /// <summary>
    /// Accurately detects the current application version by checking
    /// AssemblyInformationalVersion (which contains CI release tags), FileVersionInfo,
    /// and Assembly.GetName().Version as a fallback.
    /// </summary>
    public static Version GetApplicationVersion()
    {
        try
        {
            // 1. Try AssemblyInformationalVersion (e.g., "2.0.10.1+d19b84f...")
            var infoVerAttr = typeof(AppUpdateService).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(infoVerAttr))
            {
                string cleanVer = infoVerAttr.Split('+')[0].TrimStart('v', 'V');
                if (Version.TryParse(cleanVer, out var parsedInfo))
                {
                    return parsedInfo;
                }
            }

            // 2. Try FileVersionInfo from the running process executable
            string procPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(procPath) && File.Exists(procPath))
            {
                var fvi = FileVersionInfo.GetVersionInfo(procPath);
                string prodVer = fvi.ProductVersion?.Split('+')[0].TrimStart('v', 'V');
                if (!string.IsNullOrEmpty(prodVer) && Version.TryParse(prodVer, out var parsedProd))
                {
                    return parsedProd;
                }
                if (Version.TryParse(fvi.FileVersion, out var parsedFile))
                {
                    return parsedFile;
                }
            }

            // 3. Fallback to Assembly.GetName().Version
            return typeof(AppUpdateService).Assembly.GetName().Version ?? new Version(2, 0, 0, 0);
        }
        catch
        {
            return new Version(2, 0, 0, 0);
        }
    }

    public static async Task<bool> CheckAndStartUpdateAsync(string targetPath, Action<string> setStatus)
    {
        // 1. Check configuration to see if updates are disabled
        try
        {
            var config = new ConfigManager().LoadConfig();
            if (config.DisableAutoUpdates)
            {
                return false;
            }
        }
        catch { }

        // 2. Never auto-update developer builds running from bin folders, debuggers, or environment overrides
        if (Debugger.IsAttached ||
            targetPath.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) ||
            targetPath.Contains(@"/bin/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("TIMER2_DISABLE_UPDATE"), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("TIMER2_DISABLE_UPDATE"), "1", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. Prevent crash loops: Check cooldown marker (min 2 hours between update attempts)
        string targetDir = Path.GetDirectoryName(targetPath) ?? "";
        string cooldownPath = Path.Combine(targetDir, ".update_cooldown");
        if (File.Exists(cooldownPath))
        {
            try
            {
                var lastAttempt = File.GetLastWriteTimeUtc(cooldownPath);
                if (DateTime.UtcNow - lastAttempt < TimeSpan.FromHours(2))
                {
                    // Recent attempt made; do not loop
                    return false;
                }
            }
            catch { return false; }
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TimeRecorder/2.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        using var releaseResponse = await client.GetAsync(ReleaseApiUrl, HttpCompletionOption.ResponseHeadersRead);
        releaseResponse.EnsureSuccessStatusCode();
        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync();
        using var release = await JsonDocument.ParseAsync(releaseStream);

        string tag = release.RootElement.GetProperty("tag_name").GetString()
            ?? throw new InvalidDataException("The latest release has no version tag.");
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out Version latestVersion))
        {
            throw new InvalidDataException($"The latest release tag '{tag}' is not a valid version.");
        }

        Version currentVersion = GetApplicationVersion();
        if (latestVersion <= currentVersion)
        {
            return false;
        }

        JsonElement asset = release.RootElement.GetProperty("assets")
            .EnumerateArray()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.GetProperty("name").GetString(), UpdateAssetName, StringComparison.Ordinal));

        if (asset.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidDataException($"Release {tag} does not contain {UpdateAssetName}.");
        }

        string digest = asset.GetProperty("digest").GetString()
            ?? throw new InvalidDataException($"Release {tag} does not provide an update digest.");
        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Release {tag} has an unsupported update digest.");
        }

        string downloadUrl = asset.GetProperty("browser_download_url").GetString()
            ?? throw new InvalidDataException($"Release {tag} does not provide an update download URL.");

        // Record cooldown marker before downloading to permanently prevent restart loops
        try { File.WriteAllText(cooldownPath, DateTime.UtcNow.ToString("o")); } catch { }

        setStatus($"Downloading update {latestVersion}...");
        using var downloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("TimeRecorder/2.0");
        using var downloadResponse = await downloadClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        downloadResponse.EnsureSuccessStatusCode();

        string updatePath = Path.Combine(
            Path.GetTempPath(),
            $"TimeRecorder-update-{latestVersion}-{Guid.NewGuid():N}.exe");

        bool updateHelperStarted = false;
        try
        {
            await using (Stream download = await downloadResponse.Content.ReadAsStreamAsync())
            await using (var output = new FileStream(updatePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await download.CopyToAsync(output);
            }

            await using (var updateFile = new FileStream(updatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] actualDigest = await SHA256.HashDataAsync(updateFile);
                string actualHex = Convert.ToHexString(actualDigest);
                string expectedHex = digest["sha256:".Length..];
                if (!string.Equals(actualHex, expectedHex, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"The downloaded update for {latestVersion} failed its SHA-256 check.");
                }
            }

            setStatus("Installing update...");
            var startInfo = new ProcessStartInfo(updatePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--apply-update");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add(Path.GetFullPath(targetPath));
            using Process updateHelper = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start the update installer.");
            updateHelperStarted = true;
            return true;
        }
        finally
        {
            if (!updateHelperStarted && File.Exists(updatePath))
            {
                try { File.Delete(updatePath); } catch { }
            }
        }
    }

    public static int ApplyDownloadedUpdate(string[] args)
    {
        if (args.Length != 3 || !int.TryParse(args[1], out int parentProcessId))
        {
            MessageBox.Show(
                "The downloaded update could not be started because its arguments are invalid.",
                "Time Recorder Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }

        string targetPath = Path.GetFullPath(args[2]);
        string targetDir = Path.GetDirectoryName(targetPath) ?? "";
        string cooldownPath = Path.Combine(targetDir, ".update_cooldown");

        string sourcePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the downloaded update path.");
        string oldBackupPath = targetPath + ".old." + Guid.NewGuid().ToString("N");

        try
        {
            // 1. Wait for parent process to exit
            try
            {
                using Process parent = Process.GetProcessById(parentProcessId);
                if (!parent.WaitForExit(TimeSpan.FromSeconds(30)))
                {
                    try { parent.Kill(); } catch { }
                }
            }
            catch (ArgumentException)
            {
                // Parent already exited
            }

            // 2. Retry loop with rename-first pattern and progressive backoff
            // Windows allows renaming recently-closed executables even while handles or antivirus scanners linger.
            bool replaced = false;
            Exception lastEx = null;

            for (int attempt = 1; attempt <= 15; attempt++)
            {
                try
                {
                    Thread.Sleep(300 * attempt); // Progressive backoff (300ms, 600ms, 900ms...)

                    if (File.Exists(targetPath))
                    {
                        try { File.Move(targetPath, oldBackupPath, overwrite: true); } catch { }
                    }

                    File.Copy(sourcePath, targetPath, overwrite: true);
                    replaced = true;

                    // Clean up old backup if possible
                    try { if (File.Exists(oldBackupPath)) File.Delete(oldBackupPath); } catch { }
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lastEx = ex;
                }
            }

            if (!replaced)
            {
                // Restore old backup if copy failed
                try
                {
                    if (!File.Exists(targetPath) && File.Exists(oldBackupPath))
                    {
                        File.Move(oldBackupPath, targetPath, overwrite: true);
                    }
                }
                catch { }

                throw lastEx ?? new IOException("Could not replace executable after 15 attempts.");
            }

            // Clear cooldown marker on success
            try { if (File.Exists(cooldownPath)) File.Delete(cooldownPath); } catch { }

            // Clean up temporary downloaded updater executable
            try
            {
                if (!string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase) && File.Exists(sourcePath))
                {
                    // Mark for deletion or schedule
                }
            }
            catch { }

            // 3. Restart the updated application
            if (Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true }) == null)
            {
                throw new InvalidOperationException("The updated application could not be restarted.");
            }
            return 0;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException
                or System.ComponentModel.Win32Exception)
        {
            string errorMessage = ex.Message;

            // Ensure cooldown marker is stamped so restarting does NOT loop!
            try { File.WriteAllText(cooldownPath, DateTime.UtcNow.ToString("o")); } catch { }

            try
            {
                if (Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true }) == null)
                {
                    errorMessage += "\r\nThe current version could not be restarted.";
                }
            }
            catch (Exception restartError) when (
                restartError is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                errorMessage += $"\r\nThe current version could not be restarted: {restartError.Message}";
            }

            MessageBox.Show(
                $"The update could not be installed. The current version will be started instead.\r\n\r\n{errorMessage}",
                "Time Recorder Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 1;
        }
    }
}
