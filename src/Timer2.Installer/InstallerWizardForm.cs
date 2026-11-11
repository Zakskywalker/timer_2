using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Timer2.Installer
{
    public class InstallerWizardForm : Form
    {
        private int _currentPage = 0;
        private const int TotalPages = 7;

        // Configuration collected by wizard
        private string _targetDirectory;
        private bool _createDesktopShortcut = true;
        private bool _createStartMenuShortcut = true;
        private bool _launchOnStartup = false;
        private bool _launchNow = true;

        // UI Framework
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlBottom;
        private Button btnBack;
        private Button btnNext;
        private Button btnCancel;

        // Page Panels
        private Panel[] _pages;

        // Page 1 (License) controls
        private RadioButton rbAccept;
        private RadioButton rbDecline;

        // Page 2 (Destination) controls
        private TextBox txtInstallPath;
        private Button btnBrowse;
        private Label lblSpaceAvailable;

        // Page 3 (Tasks) controls
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkStartup;

        // Page 4 (Ready) controls
        private RichTextBox rtbReadySummary;

        // Page 5 (Installing) controls
        private ProgressBar prgInstall;
        private Label lblInstallStatus;

        // Page 6 (Complete) controls
        private CheckBox chkLaunchNow;

        public InstallerWizardForm()
        {
            this.Text = "Setup — Time Recorder 2.0";
            this.ClientSize = new Size(680, 480);
            this.MinimumSize = new Size(620, 440);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 247);
            this.Font = new Font("Segoe UI", 9f);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            _targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TimeRecorder");

            InitializeWizardLayout();
            SetupPages();
            ShowPage(0);

            this.Shown += (s, e) => {
                this.BringToFront();
                this.Activate();
                LayoutBottomButtons();
            };
            this.Resize += (s, e) => LayoutBottomButtons();
        }

        private void InitializeWizardLayout()
        {
            // Bottom Button Bar
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(238, 238, 242)
            };
            pnlBottom.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(215, 215, 220), 1), 0, 0, pnlBottom.Width, 0);
            };

            btnBack = new Button
            {
                Text = "< &Back",
                Size = new Size(85, 30),
                Font = new Font("Segoe UI", 9f)
            };
            btnBack.Click += (s, e) => { if (_currentPage > 0) ShowPage(_currentPage - 1); };

            btnNext = new Button
            {
                Text = "&Next >",
                Size = new Size(85, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnNext.Click += BtnNext_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(85, 30),
                Font = new Font("Segoe UI", 9f)
            };
            btnCancel.Click += (s, e) => {
                if (MessageBox.Show("Are you sure you want to cancel Setup?", "Exit Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.Close();
                }
            };

            pnlBottom.Controls.AddRange(new Control[] { btnBack, btnNext, btnCancel });
            pnlBottom.Resize += (s, e) => LayoutBottomButtons();

            // Left Sidebar Graphic
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(16, 24, 40)
            };
            pnlSidebar.Paint += PnlSidebar_Paint;

            // Main Content Area (Dock Fill)
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlBottom);

            // WinForms Z-Order: edge-docked panels docked first, fill panel on top
            pnlSidebar.SendToBack();
            pnlBottom.SendToBack();
            pnlContent.BringToFront();

            LayoutBottomButtons();
        }

        private void LayoutBottomButtons()
        {
            if (pnlBottom == null || btnCancel == null || btnNext == null || btnBack == null) return;
            int right = pnlBottom.ClientSize.Width - 18;
            int y = Math.Max(8, (pnlBottom.ClientSize.Height - btnCancel.Height) / 2);

            btnCancel.Location = new Point(right - btnCancel.Width, y);
            btnNext.Location = new Point(btnCancel.Left - 10 - btnNext.Width, y);
            btnBack.Location = new Point(btnNext.Left - 10 - btnBack.Width, y);

            btnCancel.BringToFront();
            btnNext.BringToFront();
            btnBack.BringToFront();
        }

        private void PnlSidebar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Sleek dark gradient
            using (var brush = new LinearGradientBrush(pnlSidebar.ClientRectangle, Color.FromArgb(10, 15, 28), Color.FromArgb(20, 32, 58), 90f))
            {
                g.FillRectangle(brush, pnlSidebar.ClientRectangle);
            }

            // Draw clock branding icon
            float cx = pnlSidebar.Width / 2.0f;
            float cy = 70;
            float r = 35;

            using (var borderPen = new Pen(Color.FromArgb(56, 189, 248), 3))
            {
                g.DrawEllipse(borderPen, cx - r, cy - r, r * 2, r * 2);
            }
            using (var faceBrush = new SolidBrush(Color.FromArgb(30, 58, 95)))
            {
                g.FillEllipse(faceBrush, cx - r + 3, cy - r + 3, (r - 3) * 2, (r - 3) * 2);
            }

            // Clock hands
            using (var handPen = new Pen(Color.White, 2.5f))
            {
                handPen.StartCap = LineCap.Round;
                handPen.EndCap = LineCap.Round;
                g.DrawLine(handPen, cx, cy, cx, cy - 20);
                g.DrawLine(handPen, cx, cy, cx + 15, cy + 5);
            }

            // Branding Text
            using (var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f))
            using (var textBrush = new SolidBrush(Color.White))
            using (var subBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("Time Recorder", titleFont, textBrush, cx, 125, sf);
                g.DrawString("Version 2.0", subFont, subBrush, cx, 148, sf);
                g.DrawString("Windows 11 Edition", subFont, textBrush, cx, 168, sf);
            }
        }

        private void SetupPages()
        {
            _pages = new Panel[TotalPages];
            for (int i = 0; i < TotalPages; i++)
            {
                _pages[i] = new Panel
                {
                    Dock = DockStyle.Fill,
                    Visible = false,
                    BackColor = Color.White,
                    Padding = new Padding(20)
                };
                pnlContent.Controls.Add(_pages[i]);
            }

            SetupPage0_Welcome();
            SetupPage1_License();
            SetupPage2_Destination();
            SetupPage3_Tasks();
            SetupPage4_Ready();
            SetupPage5_Installing();
            SetupPage6_Complete();
        }

        private void SetupPage0_Welcome()
        {
            var p = _pages[0];
            var lblTitle = new Label
            {
                Text = "Welcome to the Time Recorder 2.0 Setup Wizard",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 60),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            var lblDesc = new Label
            {
                Text = "This wizard will install Time Recorder 2.0 on your computer.\r\n\r\nFeaturing:\r\n• High-precision time recording with decimal hour tracking\r\n• Fault-tolerant auto-healing and crash recovery\r\n• System tray minimization with live runtime badges\r\n• Dynamic theme chooser (Cyber Green, Midnight, Obsidian)\r\n• MySQL dual-table synchronization and comparison\r\n• Interactive HTML reports with sticky summaries\r\n\r\nClick Next to continue, or Cancel to exit Setup.",
                Location = new Point(20, 85),
                Size = new Size(440, 280),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            p.Controls.AddRange(new Control[] { lblTitle, lblDesc });
        }

        private void SetupPage1_License()
        {
            var p = _pages[1];
            var lblTitle = new Label { Text = "License Agreement", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            var lblSub = new Label { Text = "Please read the following license terms before continuing:", Location = new Point(20, 42), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };

            var rtbLicense = new RichTextBox
            {
                Location = new Point(20, 68),
                Size = new Size(440, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 8.8f),
                Text = "GNU GENERAL PUBLIC LICENSE\nVersion 3, 29 June 2007\n\nCopyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>\n\nEveryone is permitted to copy and distribute verbatim copies of this license document, but changing it is not allowed.\n\nThis program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.\n\nThis program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details."
            };

            rbAccept = new RadioButton { Text = "I accept the agreement (GNU GPLv3)", Location = new Point(20, 280), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            rbDecline = new RadioButton { Text = "I do not accept the agreement", Location = new Point(20, 308), AutoSize = true };

            rbAccept.CheckedChanged += (s, e) => btnNext.Enabled = rbAccept.Checked;
            rbDecline.CheckedChanged += (s, e) => btnNext.Enabled = rbAccept.Checked;

            p.Controls.AddRange(new Control[] { lblTitle, lblSub, rtbLicense, rbAccept, rbDecline });
        }

        private void SetupPage2_Destination()
        {
            var p = _pages[2];
            var lblTitle = new Label { Text = "Select Destination Location", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            var lblSub = new Label { Text = "Where should Time Recorder 2.0 be installed?", Location = new Point(20, 42), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };

            var lblPrompt = new Label { Text = "To continue, click Next. To choose a different folder, click Browse.", Location = new Point(20, 85), Size = new Size(440, 25) };

            txtInstallPath = new TextBox
            {
                Text = _targetDirectory,
                Location = new Point(20, 115),
                Size = new Size(340, 28)
            };
            txtInstallPath.TextChanged += (s, e) => {
                _targetDirectory = txtInstallPath.Text.Trim();
                UpdateSpaceInfo();
            };

            btnBrowse = new Button
            {
                Text = "&Browse...",
                Location = new Point(370, 114),
                Size = new Size(90, 29)
            };
            btnBrowse.Click += (s, e) => {
                using var fbd = new FolderBrowserDialog { SelectedPath = _targetDirectory };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    txtInstallPath.Text = fbd.SelectedPath;
                }
            };

            var lblSpaceReq = new Label { Text = "At least 120.0 MB of free disk space is required.", Location = new Point(20, 160), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
            lblSpaceAvailable = new Label { Text = "Checking available disk space...", Location = new Point(20, 185), AutoSize = true, ForeColor = Color.FromArgb(30, 41, 59), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

            p.Controls.AddRange(new Control[] { lblTitle, lblSub, lblPrompt, txtInstallPath, btnBrowse, lblSpaceReq, lblSpaceAvailable });
            UpdateSpaceInfo();
        }

        private void UpdateSpaceInfo()
        {
            try
            {
                if (lblSpaceAvailable == null || string.IsNullOrWhiteSpace(_targetDirectory)) return;
                string root = Path.GetPathRoot(_targetDirectory);
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                {
                    var drive = new DriveInfo(root);
                    long freeMb = drive.AvailableFreeSpace / (1024 * 1024);
                    lblSpaceAvailable.Text = $"Available disk space: {freeMb:N0} MB";
                }
            }
            catch { }
        }

        private void SetupPage3_Tasks()
        {
            var p = _pages[3];
            var lblTitle = new Label { Text = "Select Additional Tasks", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            var lblSub = new Label { Text = "Which additional shortcuts and options should be created?", Location = new Point(20, 42), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };

            var gbShortcuts = new GroupBox
            {
                Text = "Shortcuts and Auto-Start",
                Location = new Point(20, 80),
                Size = new Size(440, 160)
            };

            chkDesktop = new CheckBox { Text = "Create a Desktop shortcut", Location = new Point(20, 35), AutoSize = true, Checked = true };
            chkStartMenu = new CheckBox { Text = "Create a Start Menu entry (Time Recorder)", Location = new Point(20, 75), AutoSize = true, Checked = true };
            chkStartup = new CheckBox { Text = "Start Time Recorder automatically on Windows boot", Location = new Point(20, 115), AutoSize = true, Checked = false };

            chkDesktop.CheckedChanged += (s, e) => _createDesktopShortcut = chkDesktop.Checked;
            chkStartMenu.CheckedChanged += (s, e) => _createStartMenuShortcut = chkStartMenu.Checked;
            chkStartup.CheckedChanged += (s, e) => _launchOnStartup = chkStartup.Checked;

            gbShortcuts.Controls.AddRange(new Control[] { chkDesktop, chkStartMenu, chkStartup });
            p.Controls.AddRange(new Control[] { lblTitle, lblSub, gbShortcuts });
        }

        private void SetupPage4_Ready()
        {
            var p = _pages[4];
            var lblTitle = new Label { Text = "Ready to Install", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            var lblSub = new Label { Text = "Setup is now ready to begin installing Time Recorder 2.0 on your computer.", Location = new Point(20, 42), Size = new Size(440, 30), ForeColor = Color.FromArgb(71, 85, 105) };

            rtbReadySummary = new RichTextBox
            {
                Location = new Point(20, 75),
                Size = new Size(440, 220),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.2f)
            };

            p.Controls.AddRange(new Control[] { lblTitle, lblSub, rtbReadySummary });
        }

        private void UpdateReadySummary()
        {
            rtbReadySummary.Text =
                $"Destination Location:\r\n  {_targetDirectory}\r\n\r\n" +
                $"Selected Tasks:\r\n" +
                $"  • Desktop Shortcut: {(_createDesktopShortcut ? "Yes" : "No")}\r\n" +
                $"  • Start Menu Shortcut: {(_createStartMenuShortcut ? "Yes" : "No")}\r\n" +
                $"  • Windows Auto-Start: {(_launchOnStartup ? "Yes" : "No")}\r\n\r\n" +
                $"License: GNU General Public License v3 (Accepted)\r\n\r\n" +
                $"Click Install to continue with the installation.";
        }

        private void SetupPage5_Installing()
        {
            var p = _pages[5];
            var lblTitle = new Label { Text = "Installing Time Recorder 2.0", Font = new Font("Segoe UI", 12f, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            var lblSub = new Label { Text = "Please wait while Setup installs Time Recorder 2.0 on your computer.", Location = new Point(20, 42), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };

            lblInstallStatus = new Label { Text = "Preparing installation...", Location = new Point(20, 100), Size = new Size(440, 24) };
            prgInstall = new ProgressBar { Location = new Point(20, 130), Size = new Size(440, 28), Minimum = 0, Maximum = 100 };

            p.Controls.AddRange(new Control[] { lblTitle, lblSub, lblInstallStatus, prgInstall });
        }

        private void SetupPage6_Complete()
        {
            var p = _pages[6];
            var lblTitle = new Label
            {
                Text = "Completing the Time Recorder 2.0 Setup Wizard",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 50),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            var lblDesc = new Label
            {
                Text = "Time Recorder 2.0 has been successfully installed on your computer.\r\n\r\nThe application may be launched using the installed desktop and start menu shortcuts or system tray integration.\r\n\r\nClick Finish to exit Setup.",
                Location = new Point(20, 80),
                Size = new Size(440, 110),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            chkLaunchNow = new CheckBox
            {
                Text = "Launch Time Recorder 2.0 now",
                Location = new Point(20, 210),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            chkLaunchNow.CheckedChanged += (s, e) => _launchNow = chkLaunchNow.Checked;

            p.Controls.AddRange(new Control[] { lblTitle, lblDesc, chkLaunchNow });
        }

        private void ShowPage(int pageIndex)
        {
            _currentPage = pageIndex;
            for (int i = 0; i < TotalPages; i++)
            {
                _pages[i].Visible = (i == pageIndex);
            }

            btnBack.Enabled = (pageIndex > 0 && pageIndex < 5);
            btnCancel.Enabled = (pageIndex < 6);

            if (pageIndex == 1)
            {
                btnNext.Enabled = rbAccept.Checked;
            }
            else
            {
                btnNext.Enabled = true;
            }

            if (pageIndex == 4)
            {
                UpdateReadySummary();
                btnNext.Text = "&Install";
            }
            else if (pageIndex == 6)
            {
                btnBack.Enabled = false;
                btnNext.Text = "&Finish";
            }
            else
            {
                btnNext.Text = "&Next >";
            }

            LayoutBottomButtons();
        }

        private async void BtnNext_Click(object sender, EventArgs e)
        {
            if (_currentPage == 4)
            {
                // Move to Installing page and run installation
                ShowPage(5);
                btnBack.Enabled = false;
                btnNext.Enabled = false;
                btnCancel.Enabled = false;

                if (await RunInstallationAsync())
                {
                    ShowPage(6);
                    btnNext.Enabled = true;
                    btnCancel.Enabled = false;
                }
                else
                {
                    ShowPage(4);
                    btnCancel.Enabled = true;
                }
            }
            else if (_currentPage == 6)
            {
                // Finished
                if (_launchNow)
                {
                    string targetExe = Path.Combine(_targetDirectory, "TimeRecorder.exe");
                    if (File.Exists(targetExe))
                    {
                        Process.Start(new ProcessStartInfo { FileName = targetExe, UseShellExecute = true });
                    }
                }
                this.Close();
            }
            else
            {
                ShowPage(_currentPage + 1);
            }
        }

        private async Task<bool> RunInstallationAsync()
        {
            void UpdateStatus(string message, int progress)
            {
                lblInstallStatus.Text = message;
                prgInstall.Value = progress;
            }

            UpdateStatus("Creating installation directory...", 10);
            await Task.Delay(200);

            if (!Directory.Exists(_targetDirectory))
            {
                Directory.CreateDirectory(_targetDirectory);
            }

            UpdateStatus("Locating application payload...", 25);
            await Task.Delay(200);

            string destExe = Path.Combine(_targetDirectory, "TimeRecorder.exe");
            string stagingExe = destExe + $".payload-{Guid.NewGuid():N}";
            bool extracted = false;
            string payloadFailure = "The embedded application payload is missing.";

            try
            {
                var asm = typeof(InstallerWizardForm).Assembly;
                using (var stream = asm.GetManifestResourceStream("Timer2.App.exe"))
                {
                    if (stream != null)
                    {
                        UpdateStatus("Extracting Time Recorder embedded payload...", 45);
                        using (var fs = new FileStream(stagingExe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fs);
                        }
                        extracted = IsValidPayload(stagingExe, stream.CanSeek ? stream.Length : null);
                        if (extracted)
                        {
                            File.Move(stagingExe, destExe, true);
                        }
                        else
                        {
                            payloadFailure = "The embedded application payload is incomplete or invalid.";
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                payloadFailure = $"Could not extract the embedded application: {ex.Message}";
                if (File.Exists(stagingExe))
                {
                    File.Delete(stagingExe);
                }
            }

            if (!extracted)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? baseDir;
                string[] searchPaths = new[]
                {
                    Path.Combine(exeDir, "Timer2.App.exe"),
                    Path.Combine(exeDir, "TimeRecorder.exe"),
                    Path.Combine(baseDir, "Timer2.App.exe"),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "publish", "win11-x64", "Timer2.App.exe")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "publish", "win11-x64", "Timer2.App.exe")),
                    Path.GetFullPath(Path.Combine(exeDir, "publish", "win11-x64", "Timer2.App.exe"))
                };

                string foundSrc = searchPaths.FirstOrDefault(p => File.Exists(p));
                if (!string.IsNullOrEmpty(foundSrc))
                {
                    try
                    {
                        UpdateStatus("Copying Time Recorder payload from disk...", 50);
                        File.Copy(foundSrc, stagingExe, true);
                        extracted = IsValidPayload(stagingExe, new FileInfo(foundSrc).Length);
                        if (extracted)
                        {
                            File.Move(stagingExe, destExe, true);
                        }
                        else
                        {
                            payloadFailure = $"The application payload at {foundSrc} is incomplete or invalid.";
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        payloadFailure = $"Could not copy the application payload: {ex.Message}";
                        if (File.Exists(stagingExe))
                        {
                            File.Delete(stagingExe);
                        }
                    }
                }
            }

            if (!extracted)
            {
                try
                {
                    await DownloadLatestApplicationAsync(destExe, UpdateStatus);
                    extracted = true;
                }
                catch (Exception ex) when (
                    ex is HttpRequestException or TaskCanceledException or IOException or InvalidDataException
                        or JsonException or InvalidOperationException or UnauthorizedAccessException
                        or KeyNotFoundException or FormatException or ArgumentException)
                {
                    payloadFailure += $"\r\n\r\nDownloading the latest app also failed: {ex.Message}";
                }
            }

            if (!extracted || !File.Exists(destExe))
            {
                MessageBox.Show(
                    $"Setup could not install the Time Recorder app.\r\n\r\n{payloadFailure}\r\n\r\n" +
                    "Check that you have a working internet connection and enough disk space, then try again.",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            // Extract or Copy License
            try
            {
                string licDest = Path.Combine(_targetDirectory, "LICENSE");
                var asm = typeof(InstallerWizardForm).Assembly;
                using (var licStream = asm.GetManifestResourceStream("LICENSE"))
                {
                    if (licStream != null)
                    {
                        using var fs = new FileStream(licDest, FileMode.Create, FileAccess.Write);
                        await licStream.CopyToAsync(fs);
                    }
                }
                if (!File.Exists(licDest))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string licSrc = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "LICENSE"));
                    if (File.Exists(licSrc)) File.Copy(licSrc, licDest, true);
                }
            }
            catch { }

            UpdateStatus("Configuring Windows 11 Shortcuts...", 75);
            await Task.Delay(250);

            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);

                    if (_createDesktopShortcut)
                    {
                        string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Time Recorder.lnk");
                        dynamic deskShortcut = shell.CreateShortcut(desktopPath);
                        deskShortcut.TargetPath = destExe;
                        deskShortcut.WorkingDirectory = _targetDirectory;
                        deskShortcut.Description = "Time Recorder 2.0";
                        deskShortcut.Save();
                    }

                    if (_createStartMenuShortcut)
                    {
                        string startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Time Recorder");
                        if (!Directory.Exists(startMenuFolder)) Directory.CreateDirectory(startMenuFolder);
                        string startMenuPath = Path.Combine(startMenuFolder, "Time Recorder.lnk");
                        dynamic smShortcut = shell.CreateShortcut(startMenuPath);
                        smShortcut.TargetPath = destExe;
                        smShortcut.WorkingDirectory = _targetDirectory;
                        smShortcut.Description = "Time Recorder 2.0 - Tracking & Logs";
                        smShortcut.Save();
                    }
                }
            }
            catch { }

            UpdateStatus("Registering Windows 11 Installed Apps entry...", 90);
            await Task.Delay(250);

            RegisterUninstallInfo(destExe);

            UpdateStatus("Installation complete!", 100);
            await Task.Delay(300);
            return true;
        }

        private static bool IsValidPayload(string path, long? expectedLength)
        {
            var file = new FileInfo(path);
            return file.Exists
                && file.Length >= 1024 * 1024
                && (!expectedLength.HasValue || file.Length == expectedLength.Value);
        }

        private static async Task DownloadLatestApplicationAsync(string destinationPath, Action<string, int> updateStatus)
        {
            const string assetName = "TimeRecorder_2.0_App.exe";
            const long maximumDownloadSize = 512L * 1024 * 1024;
            using var apiClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("TimeRecorder-Setup/2.0");
            apiClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            using var releaseResponse = await apiClient.GetAsync(
                "https://api.github.com/repos/Zakskywalker/timer_2/releases/latest",
                HttpCompletionOption.ResponseHeadersRead);
            releaseResponse.EnsureSuccessStatusCode();
            await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync();
            using var release = await JsonDocument.ParseAsync(releaseStream);

            JsonElement asset = release.RootElement.GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.GetProperty("name").GetString(), assetName, StringComparison.Ordinal));
            if (asset.ValueKind == JsonValueKind.Undefined)
            {
                throw new InvalidDataException($"The latest release does not contain {assetName}.");
            }

            long expectedLength = asset.GetProperty("size").GetInt64();
            if (expectedLength < 1024 * 1024 || expectedLength > maximumDownloadSize)
            {
                throw new InvalidDataException($"The released app size ({expectedLength} bytes) is invalid.");
            }

            string digest = asset.GetProperty("digest").GetString()
                ?? throw new InvalidDataException("The released app does not have a SHA-256 digest.");
            if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The released app has an unsupported digest.");
            }

            string downloadUrl = asset.GetProperty("browser_download_url").GetString()
                ?? throw new InvalidDataException("The released app does not have a download URL.");

            updateStatus("Downloading the latest Time Recorder app...", 35);
            using var downloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("TimeRecorder-Setup/2.0");
            using var downloadResponse = await downloadClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            downloadResponse.EnsureSuccessStatusCode();
            if (downloadResponse.Content.Headers.ContentLength.HasValue
                && downloadResponse.Content.Headers.ContentLength.Value != expectedLength)
            {
                throw new InvalidDataException("The released app download has an unexpected content length.");
            }

            string temporaryPath = destinationPath + $".download-{Guid.NewGuid():N}";
            try
            {
                await using (Stream download = await downloadResponse.Content.ReadAsStreamAsync())
                await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await download.CopyToAsync(output);
                }

                if (!IsValidPayload(temporaryPath, expectedLength))
                {
                    throw new InvalidDataException("The downloaded app has an unexpected file size.");
                }

                await using (var file = new FileStream(temporaryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string actualDigest = Convert.ToHexString(await SHA256.HashDataAsync(file));
                    if (!string.Equals(actualDigest, digest["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("The downloaded app failed its SHA-256 integrity check.");
                    }
                }

                File.Move(temporaryPath, destinationPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private void RegisterUninstallInfo(string destExe)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\TimeRecorder");
                if (key != null)
                {
                    key.SetValue("DisplayName", "Time Recorder 2.0");
                    key.SetValue("DisplayVersion", "2.0.0");
                    key.SetValue("Publisher", "Time Recorder Project (GNU GPLv3)");
                    key.SetValue("InstallLocation", _targetDirectory);
                    key.SetValue("DisplayIcon", destExe);
                    key.SetValue("UninstallString", $"powershell.exe -Command \"Remove-Item -Recurse -Force '{_targetDirectory}'; Remove-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\TimeRecorder' -Force;\"");
                    key.SetValue("NoModify", 1);
                    key.SetValue("NoRepair", 1);
                }

                if (_launchOnStartup)
                {
                    using var runKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    runKey?.SetValue("TimeRecorder", $"\"{destExe}\"");
                }
            }
            catch { }
        }
    }
}
