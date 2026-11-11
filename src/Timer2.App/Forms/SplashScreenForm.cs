using System.Drawing.Drawing2D;
using System.Reflection;
using Timer2.App.Updates;

namespace Timer2.App.Forms;

internal sealed class SplashScreenForm : Form
{
    private bool _started;
    private readonly Label _loading;
    private readonly Image _splashArtwork;

    public SplashScreenForm()
    {
        Text = "Time Recorder";
        ClientSize = new Size(740, 410);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(15, 17, 21);
        DoubleBuffered = true;

        _splashArtwork = LoadArtwork();
        var artwork = new PictureBox
        {
            Location = new Point(0, 0),
            Size = new Size(273, 410),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(10, 12, 15),
            Image = _splashArtwork
        };

        var accent = new Panel
        {
            Location = new Point(273, 0),
            Size = new Size(3, 410),
            BackColor = Color.FromArgb(224, 153, 77)
        };

        var studioName = new Label
        {
            AutoSize = true,
            Location = new Point(318, 61),
            Text = "HAND OF GLORY",
            ForeColor = Color.FromArgb(243, 189, 119),
            Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold),
            UseMnemonic = false
        };

        var studioType = new Label
        {
            AutoSize = true,
            Location = new Point(320, 91),
            Text = "GAMES & SOFTWARE",
            ForeColor = Color.FromArgb(157, 162, 170),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            UseMnemonic = false
        };

        var divider = new Panel
        {
            Location = new Point(320, 135),
            Size = new Size(355, 1),
            BackColor = Color.FromArgb(69, 61, 54)
        };

        var appName = new Label
        {
            AutoSize = true,
            Location = new Point(318, 177),
            Text = "Time Recorder",
            ForeColor = Color.FromArgb(241, 238, 232),
            Font = new Font("Segoe UI Light", 27, FontStyle.Regular),
            UseMnemonic = false
        };

        var version = new Label
        {
            AutoSize = true,
            Location = new Point(322, 222),
            Text = "VERSION 2.0",
            ForeColor = Color.FromArgb(157, 162, 170),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            UseMnemonic = false
        };

        _loading = new Label
        {
            AutoSize = true,
            Location = new Point(322, 302),
            Text = "Loading... Checking for updates",
            ForeColor = Color.FromArgb(235, 199, 157),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            UseMnemonic = false
        };

        var progress = new ProgressBar
        {
            Location = new Point(322, 333),
            Size = new Size(350, 4),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24
        };

        Controls.AddRange([artwork, accent, studioName, studioType, divider, appName, version, _loading, progress]);
        Region = CreateRoundedRegion(ClientSize, 12);
        ResizeRedraw = true;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var startupTime = System.Diagnostics.Stopwatch.StartNew();

        if (string.Equals(
            Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            _loading.Text = "Loading...";
        }
        else
        {
            try
            {
                string executablePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(executablePath)
                    && await AppUpdateService.CheckAndStartUpdateAsync(executablePath, status => _loading.Text = status))
                {
                    Close();
                    return;
                }
            }
            catch (Exception ex) when (
                ex is HttpRequestException or TaskCanceledException or IOException or InvalidDataException
                    or System.Text.Json.JsonException or InvalidOperationException or System.ComponentModel.Win32Exception
                    or KeyNotFoundException or FormatException or ArgumentException)
            {
                _loading.Text = "Update check unavailable. Starting current version...";
            }
        }

        TimeSpan remaining = TimeSpan.FromMilliseconds(1600) - startupTime.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining);
        }

        StartApplication();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _splashArtwork.Dispose();
        base.OnFormClosed(e);
    }

    private void StartApplication()
    {
        if (_started)
        {
            return;
        }
        _started = true;
        var mainForm = new MainForm();
        mainForm.FormClosed += (_, _) => Close();
        mainForm.Show();
        Hide();
    }

    private static Image LoadArtwork()
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Timer2.App.Resources.SplashArtwork.jpg")
            ?? throw new InvalidOperationException("The splash screen artwork resource is missing.");

        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static Region CreateRoundedRegion(Size size, int radius)
    {
        using var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(size.Width - diameter, 0, diameter, diameter, 270, 90);
        path.AddArc(size.Width - diameter, size.Height - diameter, diameter, diameter, 0, 90);
        path.AddArc(0, size.Height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }
}
