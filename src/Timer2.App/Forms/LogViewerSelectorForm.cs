using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Export;
using Timer2.Core.Storage;

namespace Timer2.App.Forms
{
    public class LogViewerSelectorForm : Form
    {
        private readonly ILogManager _logManager;
        private readonly ChromeActivityLog _chromeActivityLog;
        private readonly string _themeName;
        private readonly ThemePalette _palette;

        private Label lblHeaderTitle;

        // Card 1: Text Logs
        private Panel pnlTextCard;
        private RadioButton rbOptionText;
        private Label lblTextGroupTitle;
        private RadioButton rbTextTime;
        private RadioButton rbTextSystem;
        private RadioButton rbTextChanges;
        private RadioButton rbTextChrome;
        private Button btnInApp;
        private Button btnFileExplorer;

        // Card 2: HTML Report
        private Panel pnlHtmlCard;
        private RadioButton rbOptionHtml;
        private Label lblHtmlGroupTitle;
        private RadioButton rbHtmlDark;
        private RadioButton rbHtmlLight;
        private Button btnGenerateHtml;

        // Card 3: Chrome activity correlation report
        private Panel pnlChromeCard;
        private RadioButton rbOptionChrome;
        private Label lblChromeGroupTitle;
        private RadioButton rbChromeDark;
        private RadioButton rbChromeLight;
        private Button btnGenerateChromeReport;

        public LogViewerSelectorForm(ILogManager logManager, ChromeActivityLog chromeActivityLog, string themeName)
        {
            _logManager = logManager;
            _chromeActivityLog = chromeActivityLog;
            _themeName = themeName;
            _palette = ThemeManager.GetPalette(_themeName);

            this.Text = "Log Viewer";
            this.Size = new Size(580, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = _palette.Background;
            this.ForeColor = _palette.Foreground;

            InitializeComponent();
            ApplyCardStateColors();
        }

        private void InitializeComponent()
        {
            lblHeaderTitle = new Label
            {
                Text = "What logs would you like to view?",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                Location = new Point(24, 18),
                AutoSize = true,
                ForeColor = _palette.AccentLabelColor
            };
            this.Controls.Add(lblHeaderTitle);

            // ==========================================
            // CARD 1: TEXT LOG FILES
            // ==========================================
            rbOptionText = new RadioButton
            {
                Text = "Text Log Files (Raw logs && system audits)",
                Location = new Point(28, 56),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Checked = true,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            rbOptionText.CheckedChanged += (s, e) => {
                if (rbOptionText.Checked)
                {
                    rbOptionHtml.Checked = false;
                    rbOptionChrome.Checked = false;
                }
                ApplyCardStateColors();
            };
            this.Controls.Add(rbOptionText);

            pnlTextCard = new Panel
            {
                Location = new Point(46, 88),
                Size = new Size(490, 170),
                Padding = new Padding(14)
            };
            pnlTextCard.Paint += (s, e) => DrawCardBorder(pnlTextCard, e.Graphics, rbOptionText.Checked);
            pnlTextCard.Click += (s, e) => { rbOptionText.Checked = true; };

            lblTextGroupTitle = new Label
            {
                Text = "TEXT LOG OPTIONS",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(14, 12),
                AutoSize = true
            };
            lblTextGroupTitle.Click += (s, e) => { rbOptionText.Checked = true; };
            pnlTextCard.Controls.Add(lblTextGroupTitle);

            rbTextTime = new RadioButton
            {
                Text = "Time Log (time.txt)",
                Location = new Point(16, 36),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            rbTextTime.Click += (s, e) => { rbOptionText.Checked = true; };

            rbTextSystem = new RadioButton
            {
                Text = "System / Error Log (log.txt)",
                Location = new Point(185, 36),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            rbTextSystem.Click += (s, e) => { rbOptionText.Checked = true; };

            rbTextChanges = new RadioButton
            {
                Text = "Changes Audit Log (changes.txt)",
                Location = new Point(16, 64),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            rbTextChanges.Click += (s, e) => { rbOptionText.Checked = true; };

            rbTextChrome = new RadioButton
            {
                Text = "Google Chrome Activity (latest daily log)",
                Location = new Point(16, 88),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            rbTextChrome.Click += (s, e) => { rbOptionText.Checked = true; };

            btnInApp = new Button
            {
                Text = "View in App (Searchable)",
                Location = new Point(16, 118),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnInApp.Click += BtnInApp_Click;

            btnFileExplorer = new Button
            {
                Text = "Reveal in File Explorer",
                Location = new Point(220, 118),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnFileExplorer.Click += BtnFileExplorer_Click;

            pnlTextCard.Controls.AddRange(new Control[] { rbTextTime, rbTextSystem, rbTextChanges, rbTextChrome, btnInApp, btnFileExplorer });
            this.Controls.Add(pnlTextCard);

            // ==========================================
            // CARD 2: INTERACTIVE HTML REPORT
            // ==========================================
            rbOptionHtml = new RadioButton
            {
                Text = "Interactive HTML Report (Filterable table && sticky summary)",
                Location = new Point(28, 272),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Checked = false,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            rbOptionHtml.CheckedChanged += (s, e) => {
                if (rbOptionHtml.Checked)
                {
                    rbOptionText.Checked = false;
                    rbOptionChrome.Checked = false;
                }
                ApplyCardStateColors();
            };
            this.Controls.Add(rbOptionHtml);

            pnlHtmlCard = new Panel
            {
                Location = new Point(46, 304),
                Size = new Size(490, 168),
                Padding = new Padding(14)
            };
            pnlHtmlCard.Paint += (s, e) => DrawCardBorder(pnlHtmlCard, e.Graphics, rbOptionHtml.Checked);
            pnlHtmlCard.Click += (s, e) => { rbOptionHtml.Checked = true; };

            lblHtmlGroupTitle = new Label
            {
                Text = "HTML TEMPLATE THEME",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(14, 12),
                AutoSize = true
            };
            lblHtmlGroupTitle.Click += (s, e) => { rbOptionHtml.Checked = true; };
            pnlHtmlCard.Controls.Add(lblHtmlGroupTitle);

            // Separate distinct lines so labels NEVER overlap
            rbHtmlDark = new RadioButton
            {
                Text = "Dark Theme (Neon / Emerald styling)",
                Location = new Point(16, 36),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            rbHtmlDark.Click += (s, e) => { rbOptionHtml.Checked = true; };

            rbHtmlLight = new RadioButton
            {
                Text = "Light Theme (Clean slate / Minimal styling)",
                Location = new Point(16, 66),
                AutoSize = true,
                Checked = false,
                Cursor = Cursors.Hand
            };
            rbHtmlLight.Click += (s, e) => { rbOptionHtml.Checked = true; };

            btnGenerateHtml = new Button
            {
                Text = "Generate && Save HTML Report...",
                Location = new Point(16, 106),
                Size = new Size(260, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnGenerateHtml.Click += BtnGenerateHtml_Click;

            pnlHtmlCard.Controls.AddRange(new Control[] { rbHtmlDark, rbHtmlLight, btnGenerateHtml });
            this.Controls.Add(pnlHtmlCard);

            rbOptionChrome = new RadioButton
            {
                Text = "Google Chrome Activity Correlation Report",
                Location = new Point(28, 484),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            rbOptionChrome.CheckedChanged += (s, e) => {
                if (rbOptionChrome.Checked)
                {
                    rbOptionText.Checked = false;
                    rbOptionHtml.Checked = false;
                }
                ApplyCardStateColors();
            };
            this.Controls.Add(rbOptionChrome);

            pnlChromeCard = new Panel
            {
                Location = new Point(46, 516),
                Size = new Size(490, 128),
                Padding = new Padding(14)
            };
            pnlChromeCard.Paint += (s, e) => DrawCardBorder(pnlChromeCard, e.Graphics, rbOptionChrome.Checked);
            pnlChromeCard.Click += (s, e) => { rbOptionChrome.Checked = true; };

            lblChromeGroupTitle = new Label
            {
                Text = "CHROME ACTIVITY REPORT THEME",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(14, 10),
                AutoSize = true
            };
            lblChromeGroupTitle.Click += (s, e) => { rbOptionChrome.Checked = true; };

            rbChromeDark = new RadioButton
            {
                Text = "Dark Theme",
                Location = new Point(16, 34),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            rbChromeDark.Click += (s, e) => { rbOptionChrome.Checked = true; };

            rbChromeLight = new RadioButton
            {
                Text = "Light Theme",
                Location = new Point(150, 34),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            rbChromeLight.Click += (s, e) => { rbOptionChrome.Checked = true; };

            btnGenerateChromeReport = new Button
            {
                Text = "Generate && Save Chrome Correlation Report...",
                Location = new Point(16, 76),
                Size = new Size(340, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnGenerateChromeReport.Click += BtnGenerateChromeReport_Click;

            pnlChromeCard.Controls.AddRange(new Control[]
            {
                lblChromeGroupTitle,
                rbChromeDark,
                rbChromeLight,
                btnGenerateChromeReport
            });
            this.Controls.Add(pnlChromeCard);
        }

        private void DrawCardBorder(Panel panel, Graphics g, bool isActive)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Border color: vibrant theme accent when active, visible hazy muted gray when inactive
            Color borderColor = isActive 
                ? _palette.AccentLabelColor 
                : Color.FromArgb(70, 80, 95);

            int borderWidth = isActive ? 2 : 1;

            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }

        /// <summary>
        /// Styles the active option card with full vibrant theme colors,
        /// and applies a readable muted-gray hazy style to the inactive card.
        /// </summary>
        private void ApplyCardStateColors()
        {
            bool textActive = rbOptionText.Checked;
            bool htmlActive = rbOptionHtml.Checked;
            bool chromeActive = rbOptionChrome.Checked;

            // Header Radio Buttons
            rbOptionText.ForeColor = textActive ? _palette.AccentLabelColor : Color.FromArgb(140, 150, 165);
            rbOptionHtml.ForeColor = htmlActive ? _palette.AccentLabelColor : Color.FromArgb(140, 150, 165);
            rbOptionChrome.ForeColor = chromeActive ? _palette.AccentLabelColor : Color.FromArgb(140, 150, 165);

            // Palette colors
            Color activeFg = _palette.Foreground;
            Color activeAccent = _palette.AccentLabelColor;
            Color inactiveFg = Color.FromArgb(155, 165, 180);     // Clear visible muted gray
            Color inactiveGroupTitle = Color.FromArgb(115, 125, 140);
            Color inactiveBg = Color.FromArgb(16, 20, 28);       // Faint hazy card background
            Color activeBg = Color.FromArgb(8, 12, 18);

            // ----------------------------------------------------
            // Style Text Card
            // ----------------------------------------------------
            pnlTextCard.BackColor = textActive ? activeBg : inactiveBg;
            lblTextGroupTitle.ForeColor = textActive ? activeAccent : inactiveGroupTitle;

            rbTextTime.ForeColor = textActive ? activeFg : inactiveFg;
            rbTextSystem.ForeColor = textActive ? activeFg : inactiveFg;
            rbTextChanges.ForeColor = textActive ? activeFg : inactiveFg;
            rbTextChrome.ForeColor = textActive ? activeFg : inactiveFg;

            if (textActive)
            {
                StyleButton(btnInApp, _palette.ButtonBackground, _palette.ButtonForeground, _palette.ButtonBorderColor, true);
                StyleButton(btnFileExplorer, _palette.ButtonBackground, _palette.ButtonForeground, _palette.ButtonBorderColor, true);
            }
            else
            {
                // Muted gray hazy button - perfectly visible, but styled as inactive
                StyleButton(btnInApp, Color.FromArgb(28, 35, 45), Color.FromArgb(145, 155, 170), Color.FromArgb(55, 65, 80), false);
                StyleButton(btnFileExplorer, Color.FromArgb(28, 35, 45), Color.FromArgb(145, 155, 170), Color.FromArgb(55, 65, 80), false);
            }
            pnlTextCard.Invalidate();

            // ----------------------------------------------------
            // Style HTML Card
            // ----------------------------------------------------
            pnlHtmlCard.BackColor = htmlActive ? activeBg : inactiveBg;
            lblHtmlGroupTitle.ForeColor = htmlActive ? activeAccent : inactiveGroupTitle;

            rbHtmlDark.ForeColor = htmlActive ? activeFg : inactiveFg;
            rbHtmlLight.ForeColor = htmlActive ? activeFg : inactiveFg;

            if (htmlActive)
            {
                StyleButton(btnGenerateHtml, _palette.ButtonBackground, _palette.ButtonForeground, _palette.ButtonBorderColor, true);
            }
            else
            {
                StyleButton(btnGenerateHtml, Color.FromArgb(28, 35, 45), Color.FromArgb(145, 155, 170), Color.FromArgb(55, 65, 80), false);
            }
            pnlHtmlCard.Invalidate();

            pnlChromeCard.BackColor = chromeActive ? activeBg : inactiveBg;
            lblChromeGroupTitle.ForeColor = chromeActive ? activeAccent : inactiveGroupTitle;
            rbChromeDark.ForeColor = chromeActive ? activeFg : inactiveFg;
            rbChromeLight.ForeColor = chromeActive ? activeFg : inactiveFg;
            if (chromeActive)
            {
                StyleButton(btnGenerateChromeReport, _palette.ButtonBackground, _palette.ButtonForeground, _palette.ButtonBorderColor, true);
            }
            else
            {
                StyleButton(btnGenerateChromeReport, Color.FromArgb(28, 35, 45), Color.FromArgb(145, 155, 170), Color.FromArgb(55, 65, 80), false);
            }
            pnlChromeCard.Invalidate();
        }

        private void StyleButton(Button btn, Color bg, Color fg, Color border, bool isHighPriority)
        {
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.BorderSize = 1;
        }

        private string GetSelectedLogFilePath(out string title)
        {
            if (rbTextChrome.Checked)
            {
                DateTime day = _chromeActivityLog.GetAvailableDays().FirstOrDefault(DateTime.Today);
                title = $"Google Chrome Activity ({day:yyyy-MM-dd})";
                return _chromeActivityLog.GetPathForDay(day);
            }
            if (rbTextSystem.Checked)
            {
                title = "System & Error Log";
                return _logManager.SystemLogPath;
            }
            if (rbTextChanges.Checked)
            {
                title = "Changes Audit Log";
                return _logManager.ChangesLogPath;
            }
            title = "Time Log";
            return _logManager.TimeLogPath;
        }

        private void BtnInApp_Click(object sender, EventArgs e)
        {
            rbOptionText.Checked = true;
            string path = GetSelectedLogFilePath(out string title);
            var viewer = new InAppLogViewerForm(path, title, _themeName);
            viewer.Show(this);
        }

        private void BtnFileExplorer_Click(object sender, EventArgs e)
        {
            rbOptionText.Checked = true;
            string path = GetSelectedLogFilePath(out _);
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else if (rbTextChrome.Checked && Directory.Exists(_chromeActivityLog.DirectoryPath))
            {
                Process.Start("explorer.exe", $"\"{_chromeActivityLog.DirectoryPath}\"");
            }
            else if (Directory.Exists(_logManager.LogDirectory))
            {
                Process.Start("explorer.exe", $"\"{_logManager.LogDirectory}\"");
            }
            else
            {
                MessageBox.Show("Log directory does not exist yet.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnGenerateHtml_Click(object sender, EventArgs e)
        {
            rbOptionHtml.Checked = true;
            string theme = rbHtmlDark.Checked ? "Dark" : "Light";

            using var sfd = new SaveFileDialog
            {
                Title = "Choose HTML Report Location",
                Filter = "HTML Document (*.html)|*.html",
                FileName = $"TimerReport_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                InitialDirectory = _logManager.LogDirectory
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var days = _logManager.GetAllDays();
                    var generator = new HtmlReportGenerator();
                    generator.SaveReportToFile(days, sfd.FileName, theme);

                    var res = MessageBox.Show(
                        $"HTML Report successfully created at:\r\n{sfd.FileName}\r\n\r\nWould you like to open it in your web browser now?",
                        "Report Created",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (res == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate HTML report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnGenerateChromeReport_Click(object sender, EventArgs e)
        {
            rbOptionChrome.Checked = true;
            string theme = rbChromeDark.Checked ? "Dark" : "Light";

            using var sfd = new SaveFileDialog
            {
                Title = "Choose Chrome Activity Report Location",
                Filter = "HTML Document (*.html)|*.html",
                FileName = $"ChromeActivityReport_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                InitialDirectory = _logManager.LogDirectory
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var chromeEntries = _chromeActivityLog.GetAllEntries();
                if (chromeEntries.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "No Chrome activity has been recorded yet.",
                        "No Chrome Activity",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var generator = new ChromeActivityReportGenerator();
                generator.SaveReportToFile(chromeEntries, _logManager.GetAllDays(), sfd.FileName, theme);

                DialogResult openReport = MessageBox.Show(
                    this,
                    $"Chrome activity correlation report created at:\r\n{sfd.FileName}\r\n\r\nWould you like to open it now?",
                    "Report Created",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (openReport == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is System.Text.Json.JsonException ||
                ex is System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    this,
                    $"Failed to generate Chrome activity report: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
