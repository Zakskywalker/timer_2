using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace Timer2.App.Updates;

internal static class AppUpdateService
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/Zakskywalker/timer_2/releases/latest";
    private const string UpdateAssetName = "TimeRecorder_2.0_App.exe";

    public static async Task<bool> CheckAndStartUpdateAsync(string targetPath, Action<string> setStatus)
    {
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

        Version currentVersion = typeof(AppUpdateService).Assembly.GetName().Version ?? new Version(0, 0);
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
                File.Delete(updatePath);
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
        string sourcePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the downloaded update path.");
        string replacementPath = targetPath + ".update";

        try
        {
            try
            {
                using Process parent = Process.GetProcessById(parentProcessId);
                if (!parent.WaitForExit(TimeSpan.FromMinutes(2)))
                {
                    throw new TimeoutException("Time Recorder did not close in time to apply the update.");
                }
            }
            catch (ArgumentException)
            {
                // The application exited before the update helper started.
            }

            File.Copy(sourcePath, replacementPath, overwrite: true);
            File.Move(replacementPath, targetPath, overwrite: true);
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
            try
            {
                if (File.Exists(replacementPath))
                {
                    File.Delete(replacementPath);
                }

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
                $"The update could not be installed. The current version will be started if possible.\r\n\r\n{errorMessage}",
                "Time Recorder Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }
}
