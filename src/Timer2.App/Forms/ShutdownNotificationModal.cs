using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Calculations;

namespace Timer2.App.Forms
{
    /// <summary>
    /// Alert-style modal dialog displayed on application shutdown.
    /// Matches the active main form theme, displays a styled warning/alert header,
    /// presents the daily console session summary (with clock timestamps stripped),
    /// and restates the path to local persistent logs.
    /// </summary>
    public sealed class ShutdownNotificationModal : Form
    {
        private Panel pnlWarningBanner;
        private Label lblWarningIcon;
        private Label lblWarningTitle;
        private Label lblWarningSub;
        private Label lblConsoleHeader;
        private RichTextBox rtbConsoleSummary;
        private GroupBox gbLogsLocation;
        private Label lblLogsNotice;
        private TextBox txtLogDirectory;
        private Label lblLogFiles;
        private Button btnOk;

        public ShutdownNotificationModal(
            string rawConsoleText,
            string logDirectory,
            string timeLogPath,
            string themeName)
        {
            InitializeComponent(rawConsoleText, logDirectory, timeLogPath);
            ApplyStyling(themeName);
            this.FormClosing += shutdownNotificationModal_FormClosing;
        }
    private void shutdownNotificationModal_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
           Application.Exit();
        }
    }
        private void InitializeComponent(string rawConsoleText, string logDirectory, string timeLogPath)
        {
            this.Text = "Time Recorder 2.0 — Application Shutdown Notice";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;

            int clientW = 564;
            int pad = 16;
            int curY = 14;

            // ---- 1. Alert / Warning Styled Top Banner ----
            pnlWarningBanner = new Panel
            {
                Location = new Point(pad, curY),
                Size = new Size(clientW - pad * 2, 54),
                BackColor = Color.FromArgb(52, 40, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblWarningIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 16f, FontStyle.Regular),
                Location = new Point(8, 8),
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblWarningTitle = new Label
            {
                Text = "Timer Shutdown Notification",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(48, 6),
                AutoSize = true
            };

            lblWarningSub = new Label
            {
                Text = "The application is closing. Review today's session summary and verified log store below.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(230, 230, 230),
                Location = new Point(48, 28),
                Size = new Size(pnlWarningBanner.Width - 56, 20)
            };

            pnlWarningBanner.Controls.Add(lblWarningIcon);
            pnlWarningBanner.Controls.Add(lblWarningTitle);
            pnlWarningBanner.Controls.Add(lblWarningSub);
            this.Controls.Add(pnlWarningBanner);
            curY += pnlWarningBanner.Height + 12;

            // ---- 2. Daily Summary (Minus Timestamps) ----
            lblConsoleHeader = new Label
            {
                Text = "Session Summary (Daily Output without Timestamps):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(pad, curY),
                AutoSize = true
            };
            this.Controls.Add(lblConsoleHeader);
            curY += lblConsoleHeader.Height + 4;

            rtbConsoleSummary = new RichTextBox
            {
                Location = new Point(pad, curY),
                Size = new Size(clientW - pad * 2, 175),
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = ConsoleOutputFilter.FilterMinusTimestamps(rawConsoleText)
            };
            this.Controls.Add(rtbConsoleSummary);
            curY += rtbConsoleSummary.Height + 12;

            // ---- 3. Local Logs Location Restatement ----
            gbLogsLocation = new GroupBox
            {
                Text = "📁 Local Logs Directory",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(pad, curY),
                Size = new Size(clientW - pad * 2, 105)
            };

            lblLogsNotice = new Label
            {
                Text = "Daily recordings, time adjustments, and activity logs are permanently saved to:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Location = new Point(12, 22),
                AutoSize = true
            };
            gbLogsLocation.Controls.Add(lblLogsNotice);

            txtLogDirectory = new TextBox
            {
                Text = logDirectory,
                Location = new Point(12, 44),
                Size = new Size(gbLogsLocation.Width - 24, 23),
                ReadOnly = true,
                Font = new Font("Segoe UI", 9f)
            };
            gbLogsLocation.Controls.Add(txtLogDirectory);

            string timeFileOnly = string.IsNullOrEmpty(timeLogPath) ? "time.txt" : Path.GetFileName(timeLogPath);
            lblLogFiles = new Label
            {
                Text = $"Verified Primary Log: {timeFileOnly} (UTF-8 tab-delimited daily ledger)",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                Location = new Point(12, 73),
                AutoSize = true
            };
            gbLogsLocation.Controls.Add(lblLogFiles);
            this.Controls.Add(gbLogsLocation);
            curY += gbLogsLocation.Height + 14;

            // ---- 4. OK Button ----
            btnOk = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(130, 34),
                Location = new Point((clientW - 130) / 2, curY),
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) => this.Close();
            this.Controls.Add(btnOk);

            this.AcceptButton = btnOk;
            this.CancelButton = btnOk;
        }

        private void ApplyStyling(string themeName)
        {
            ThemeManager.ApplyTheme(this, themeName);

            ThemePalette palette = ThemeManager.Themes.TryGetValue(themeName, out var p)
                ? p
                : ThemeManager.Themes["CyberGreen"];

            // Preserve the distinct warm warning tone for the alert panel header
            bool isDark = palette.Background.R < 100 && palette.Background.G < 100 && palette.Background.B < 100;
            if (isDark)
            {
                pnlWarningBanner.BackColor = Color.FromArgb(48, 38, 12);
                lblWarningTitle.ForeColor = Color.FromArgb(251, 191, 36); // Amber / Gold
                lblWarningSub.ForeColor = Color.FromArgb(220, 220, 220);
                rtbConsoleSummary.BackColor = palette.LogBoxBackground;
                rtbConsoleSummary.ForeColor = palette.LogBoxForeground;
            }
            else
            {
                pnlWarningBanner.BackColor = Color.FromArgb(254, 243, 199); // Soft amber light
                lblWarningTitle.ForeColor = Color.FromArgb(146, 64, 14);
                lblWarningSub.ForeColor = Color.FromArgb(75, 85, 99);
                rtbConsoleSummary.BackColor = palette.LogBoxBackground;
                rtbConsoleSummary.ForeColor = palette.LogBoxForeground;
            }

            btnOk.BackColor = palette.ButtonBackground;
            btnOk.ForeColor = palette.ButtonForeground;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderColor = palette.ButtonBorderColor;
            btnOk.FlatAppearance.BorderSize = 1;
        }
    }
}
