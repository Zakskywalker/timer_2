using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer2.App.Services;
using Timer2.App.UI;
using Timer2.Core.Calculations;
using Timer2.Core.Database;
using Timer2.Core.Models;
using Timer2.Core.Storage;
using System.Globalization;
using System.Diagnostics;

namespace Timer2.App.Forms
{
    public class MainForm : Form
    {
        private readonly ConfigManager _configManager;
        private AppConfig _config;
        private ILogManager _logManager;
        private ChromeActivityRecorder _chromeActivityRecorder;
        private ChromeActivityForm _chromeActivityForm;
        private System.Windows.Forms.Timer _tickTimer;
        private System.Windows.Forms.Timer _checkpointTimer;

        // Active session state
        private bool _isRecording = false;
        private DateTime _currentStartTime;
        private TimeSpan _accumulatedDayTotalBeforeCurrent = TimeSpan.Zero;
        private int _currentRunNumber = 1;

        // UI Controls
        private MenuStrip menuStrip;
        private Label lblOutputDirectory;
        private TextBox txtOutputDirectory;
        private Button btnSetDirectory;
        private CheckBox chkSaveLocation;

        private Panel pnlWarningAlert;
        private Label lblWarningAlert;
        private Button btnFixWarnings;

        private Button btnStartStop;
        private Button btnTotal;
        private Button btnAddModifyTime;
        private Button btnLogViewer;
        private Button btnChromeActivity;

        private Label lblLog;
        private RichTextBox rtbConsole;

        private Panel pnlCopyBar;
        private Label lblCopyFormat;
        private ComboBox cbCopyFormat;
        private Button btnCopyTotal;
        private Button btnReloadToday;
        private Label lblCopyFeedback;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private Icon _iconBlue;
        private Icon _iconRed;
        private bool _isExplicitExit = false;
        private bool _shutdownModalShown = false;

        public MainForm()
        {
            _configManager = new ConfigManager();
            _config = _configManager.LoadConfig();

            _iconBlue = ClockIconFactory.CreateClockIcon(false, 32);
            _iconRed = ClockIconFactory.CreateClockIcon(true, 32);

            this.Icon = _iconBlue;
            this.Text = "Time Recorder";
            
            // Default size matching original screenshot, but now stretchable in all directions and into a phone bar!
            this.Size = new Size(510, 420);
            this.MinimumSize = new Size(280, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            _logManager = new LogManager(_config.OutputDirectory, _config.SaveCustomLocation);
            _chromeActivityRecorder = new ChromeActivityRecorder(new Timer2.Core.Storage.ChromeActivityLog(_logManager));

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _config.Theme);

            SetupTimers();
            SetupTrayIcon();

            // Refresh Day totals and check for anomalies
            RecalculateDayTotals();
            CheckMissingTimesAnomaly();
            UpdateConsoleDisplay();

            this.Shown += MainForm_Shown;
            this.Resize += (s, e) => PerformResponsiveLayout();
            PerformResponsiveLayout();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            CheckCrashRecovery();
        }

        private void InitializeComponent()
        {
            // Menu
            menuStrip = new MenuStrip { Dock = DockStyle.Top };
            var fileMenu = new ToolStripMenuItem("File");

            var settingsItem = new ToolStripMenuItem("Settings", null, (s, e) => OpenSettings());
            var syncCompareItem = new ToolStripMenuItem("Database Sync & Compare...", null, (s, e) => OpenDbSyncComparison());
            var mysqlItem = new ToolStripMenuItem("MySQL Configuration...", null, (s, e) => OpenMySqlConfig());
            var importItem = new ToolStripMenuItem("Import Old Logs...", null, (s, e) => ImportOldLogs());
            var aboutItem = new ToolStripMenuItem("About & License (GPLv3)", null, (s, e) => ShowAboutLicense());
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => CloseApplication());

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { 
                settingsItem, 
                syncCompareItem, 
                mysqlItem, 
                importItem, 
                new ToolStripSeparator(), 
                aboutItem, 
                new ToolStripSeparator(), 
                exitItem 
            });
            menuStrip.Items.Add(fileMenu);
            this.Controls.Add(menuStrip);

            // Directory controls
            lblOutputDirectory = new Label
            {
                Text = "Set Output Directory:",
                Location = new Point(14, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(lblOutputDirectory);

            txtOutputDirectory = new TextBox
            {
                Text = _config.OutputDirectory,
                Location = new Point(14, 50),
                Font = new Font("Segoe UI", 9f),
                ReadOnly = true
            };

            btnSetDirectory = new Button
            {
                Text = "Set",
                Location = new Point(430, 48),
                Width = 60,
                Height = 26,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnSetDirectory.Click += BtnSetDirectory_Click;
            this.Controls.AddRange(new Control[] { txtOutputDirectory, btnSetDirectory });

            chkSaveLocation = new CheckBox
            {
                Text = "Save Custom Log Location",
                Location = new Point(14, 78),
                AutoSize = true,
                Checked = _config.SaveCustomLocation,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            chkSaveLocation.CheckedChanged += (s, e) => {
                _config.SaveCustomLocation = chkSaveLocation.Checked;
                _configManager.SaveConfig(_config);
            };
            this.Controls.Add(chkSaveLocation);

            // Warning Banner for Missing Times
            pnlWarningAlert = new Panel
            {
                Location = new Point(14, 102),
                Size = new Size(475, 26),
                BackColor = Color.FromArgb(60, 40, 0),
                Visible = false
            };
            lblWarningAlert = new Label
            {
                Text = "⚠️ Incomplete times detected in log records.",
                ForeColor = Color.Gold,
                Location = new Point(6, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnFixWarnings = new Button
            {
                Text = "Review & Fix",
                Location = new Point(375, 2),
                Size = new Size(95, 22),
                BackColor = Color.Gold,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnFixWarnings.Click += (s, e) => OpenAddModifyTime();
            pnlWarningAlert.Controls.Add(lblWarningAlert);
            pnlWarningAlert.Controls.Add(btnFixWarnings);
            this.Controls.Add(pnlWarningAlert);

            // Action Buttons
            btnStartStop = new Button
            {
                Text = "Start",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            btnStartStop.Click += BtnStartStop_Click;

            btnTotal = new Button
            {
                Text = "Total",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            btnTotal.Click += BtnTotal_Click;

            btnAddModifyTime = new Button
            {
                Text = "Add/Modify\r\nTime",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnAddModifyTime.Click += (s, e) => OpenAddModifyTime();

            btnLogViewer = new Button
            {
                Text = "Log\r\nViewer",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnLogViewer.Click += (s, e) => OpenLogViewer();

            this.Controls.AddRange(new Control[] { btnStartStop, btnTotal, btnAddModifyTime, btnLogViewer });

            // Log Label
            lblLog = new Label
            {
                Text = "Log:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(lblLog);

            // Console Box - tightened height (80px) to eliminate extra blank spacing
            rtbConsole = new RichTextBox
            {
                Height = 82,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9.2f),
                Tag = "LogBox"
            };
            this.Controls.Add(rtbConsole);

            btnChromeActivity = new Button
            {
                Text = "Google Chrome Activity",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnChromeActivity.Click += (s, e) => OpenChromeActivity();
            this.Controls.Add(btnChromeActivity);

            // Copy Bar & Reload Button under Log
            pnlCopyBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(8, 6, 8, 6)
            };

            lblCopyFormat = new Label
            {
                Text = "Format:",
                Location = new Point(8, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            cbCopyFormat = new ComboBox
            {
                Location = new Point(60, 8),
                Width = 145,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbCopyFormat.Items.Add(new CopyFormatItem { Key = "HP", Display = "H:P (e.g. 4.25 hrs)" });
            cbCopyFormat.Items.Add(new CopyFormatItem { Key = "HM", Display = "H:M (e.g. 4 Hrs. 15 Min.)" });
            cbCopyFormat.SelectedIndex = _config.DefaultCopyFormat == "HM" ? 1 : 0;
            cbCopyFormat.SelectedIndexChanged += (s, e) => {
                if (cbCopyFormat.SelectedItem is CopyFormatItem item)
                {
                    _config.DefaultCopyFormat = item.Key;
                    _configManager.SaveConfig(_config);
                }
            };

            btnCopyTotal = new Button
            {
                Text = "Copy Total",
                Location = new Point(212, 6),
                Size = new Size(84, 28),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnCopyTotal.Click += BtnCopyTotal_Click;

            btnReloadToday = new Button
            {
                Text = "Reload Today",
                Location = new Point(302, 6),
                Size = new Size(95, 28),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnReloadToday.Click += BtnReloadToday_Click;

            lblCopyFeedback = new Label
            {
                Text = "",
                Location = new Point(405, 12),
                AutoSize = true,
                ForeColor = Color.LimeGreen,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };

            pnlCopyBar.Controls.AddRange(new Control[] { lblCopyFormat, cbCopyFormat, btnCopyTotal, btnReloadToday, lblCopyFeedback });
            this.Controls.Add(pnlCopyBar);

            this.FormClosing += MainForm_FormClosing;
        }

        /// <summary>
        /// Responsive layout manager supporting both wide stretched desktop mode and thin "phone bar" mode.
        /// </summary>
        private void PerformResponsiveLayout()
        {
            int clientW = this.ClientSize.Width;
            int clientH = this.ClientSize.Height;
            if (clientW <= 0 || clientH <= 0) return;

            int pad = 14;

            // Directory controls
            txtOutputDirectory.Location = new Point(pad, 50);
            txtOutputDirectory.Width = clientW - pad * 2 - 68;
            btnSetDirectory.Location = new Point(clientW - pad - 62, 49);
            btnSetDirectory.Width = 62;

            int curY = chkSaveLocation.Bottom + 6;

            if (pnlWarningAlert.Visible)
            {
                pnlWarningAlert.Location = new Point(pad, curY);
                pnlWarningAlert.Width = clientW - pad * 2;
                btnFixWarnings.Location = new Point(pnlWarningAlert.Width - 100, 2);
                curY += pnlWarningAlert.Height + 6;
            }

            bool isPhoneBar = clientW < 400;

            if (isPhoneBar)
            {
                // Bar phone mode: 2x2 grid of buttons
                int gap = 8;
                int bW = (clientW - (pad * 2) - gap) / 2;
                int bH = 38;

                // Row 1
                btnStartStop.SetBounds(pad, curY, bW, bH);
                btnTotal.SetBounds(pad + bW + gap, curY, bW, bH);

                // Row 2
                curY += bH + 6;
                btnAddModifyTime.SetBounds(pad, curY, bW, bH);
                btnLogViewer.SetBounds(pad + bW + gap, curY, bW, bH);

                curY += bH + 8;
            }
            else
            {
                // Wide / Desktop mode: 1 row of 4 buttons
                int gap = 10;
                int bW = (clientW - (pad * 2) - (gap * 3)) / 4;
                int bH = 44;

                btnStartStop.SetBounds(pad, curY, bW, bH);
                btnTotal.SetBounds(pad + (bW + gap), curY, bW, bH);
                btnAddModifyTime.SetBounds(pad + (bW + gap) * 2, curY, bW, bH);
                btnLogViewer.SetBounds(pad + (bW + gap) * 3, curY, bW, bH);

                curY += bH + 10;
            }

            // Log label
            lblLog.Location = new Point(pad, curY);
            curY += lblLog.Height + 4;

            // RichTextBox - responsive height and width
            const int chromeButtonHeight = 28;
            const int chromeButtonGap = 5;
            int availableHeight = pnlCopyBar.Top - curY - 8 - chromeButtonHeight - chromeButtonGap;
            int rtbHeight = Math.Max(40, Math.Min(160, availableHeight));

            rtbConsole.Location = new Point(pad, curY);
            rtbConsole.Size = new Size(clientW - pad * 2, rtbHeight);
            btnChromeActivity.SetBounds(
                pad,
                rtbConsole.Bottom + chromeButtonGap,
                Math.Min(210, clientW - pad * 2),
                chromeButtonHeight);

            // Bottom bar responsive arrangement
            if (isPhoneBar)
            {
                pnlCopyBar.Height = 65;
                lblCopyFormat.Location = new Point(pad, 8);
                cbCopyFormat.SetBounds(60, 6, 120, 24);
                lblCopyFeedback.Location = new Point(185, 10);

                btnCopyTotal.SetBounds(pad, 34, (clientW - pad * 2 - 8) / 2, 26);
                btnReloadToday.SetBounds(btnCopyTotal.Right + 8, 34, (clientW - pad * 2 - 8) / 2, 26);
            }
            else
            {
                pnlCopyBar.Height = 44;
                lblCopyFormat.Location = new Point(pad, 12);
                cbCopyFormat.SetBounds(62, 8, 140, 24);
                btnCopyTotal.SetBounds(210, 6, 85, 28);
                btnReloadToday.SetBounds(300, 6, 95, 28);
                lblCopyFeedback.Location = new Point(402, 12);
            }
        }

        private void SetupTimers()
        {
            _tickTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _tickTimer.Tick += TickTimer_Tick;

            _checkpointTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(2, _config.AutoCheckpointSeconds) * 1000
            };
            _checkpointTimer.Tick += CheckpointTimer_Tick;
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open Time Recorder", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add(new ToolStripSeparator());
            var trayStartStop = new ToolStripMenuItem("Start / Stop", null, (s, e) => BtnStartStop_Click(this, EventArgs.Empty));
            trayMenu.Items.Add(trayStartStop);
            trayMenu.Items.Add("Log Viewer", null, (s, e) => OpenLogViewer());
            trayMenu.Items.Add("Google Chrome Activity", null, (s, e) => OpenChromeActivity());
            trayMenu.Items.Add("Database Sync...", null, (s, e) => OpenDbSyncComparison());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, (s, e) => CloseApplication());

            trayIcon = new NotifyIcon
            {
                Icon = _iconBlue,
                Text = "Time Recorder - Stopped",
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void RecalculateDayTotals()
        {
            var todaySummary = _logManager.GetTodaySummary();
            Debug.WriteLine($"Recalculated today's totals: {TimeCalculator.FormatHM(todaySummary.TotalTime, true)} ({TimeCalculator.FormatDecimalHours(todaySummary.TotalTime)})");
            _accumulatedDayTotalBeforeCurrent = todaySummary.TotalTime;
            _currentRunNumber = todaySummary.Entries.Count + 1;
        }

        private void CheckMissingTimesAnomaly()
        {
            var days = _logManager.GetAllDays();
            int totalWarnings = days.Sum(d => d.WarningCount);

            if (totalWarnings > 0)
            {
                pnlWarningAlert.Visible = true;
                lblWarningAlert.Text = $"⚠️ Notice: {totalWarnings} incomplete session(s) in logs.";
            }
            else
            {
                pnlWarningAlert.Visible = false;
            }
            PerformResponsiveLayout();
        }

        private void CheckCrashRecovery()
        {
            var recovery = _logManager.DetectCrashOrUnclosedSession();
            if (recovery.HasUnclosedSession)
            {
                using var dlg = new AutoHealRecoveryDialog(recovery, _config.Theme);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (dlg.SelectedAction == AutoHealAction.ContinueRecording)
                    {
                        ResumeTimer(recovery.StartTime);
                        ShowFeedback("Resumed previous active recording!");
                    }
                    else if (dlg.SelectedAction == AutoHealAction.CloseLastTimestamp)
                    {
                        _logManager.AutoHealCloseLastTimestamp(recovery.StartTime, dlg.ChosenStopTime, out var dur, out var tot);
                        RecalculateDayTotals();
                        UpdateConsoleDisplay();
                        ShowFeedback($"Closed session: {TimeCalculator.FormatHM(dur, true)}");
                    }
                    else
                    {
                        _logManager.AutoHealDiscardSession();
                        RecalculateDayTotals();
                        UpdateConsoleDisplay();
                        ShowFeedback("Discarded interrupted session.");
                    }
                }
            }
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (!_isRecording)
            {
                StartTimer(DateTime.Now);
            }
            else
            {
                StopTimer();
            }
        }

        private void StartTimer(DateTime startTime)
        {
            _isRecording = true;
            _currentStartTime = startTime;
            RecalculateDayTotals();

            _logManager.RecordStart(_currentStartTime);

            btnStartStop.Text = "Stop";
            this.Icon = _iconRed;
            trayIcon.Icon = _iconRed;

            _tickTimer.Start();
            _checkpointTimer.Start();

            UpdateTrayTooltip();
            UpdateConsoleDisplay();
        }

        private void ResumeTimer(DateTime recoveredStart)
        {
            _isRecording = true;
            _currentStartTime = recoveredStart;
            RecalculateDayTotals();

            btnStartStop.Text = "Stop";
            this.Icon = _iconRed;
            trayIcon.Icon = _iconRed;

            _tickTimer.Start();
            _checkpointTimer.Start();

            UpdateTrayTooltip();
            UpdateConsoleDisplay();
        }

        private void StopTimer()
        {
            _isRecording = false;
            _tickTimer.Stop();
            _checkpointTimer.Stop();

            DateTime stopTime = DateTime.Now;
            TimeSpan runDuration = stopTime - _currentStartTime;
            TimeSpan todayTotal = _accumulatedDayTotalBeforeCurrent + runDuration;

            _logManager.RecordStop(_currentStartTime, stopTime, runDuration, todayTotal);

            btnStartStop.Text = "Start";
            this.Icon = _iconBlue;
            trayIcon.Icon = _iconBlue;

            RecalculateDayTotals();
            UpdateTrayTooltip();
            UpdateConsoleDisplay();

            // Auto sync to MySQL if enabled
            if (_config.MySql.Enabled && _config.MySql.AutoSyncOnStop)
            {
                _ = Task.Run(async () => {
                    var connector = new MySqlDataConnector(_config.MySql);
                    await connector.SyncSessionsAsync(_logManager.GetAllDays());
                });
            }
        }

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            UpdateTrayTooltip();
            UpdateConsoleDisplay();
        }

        private void CheckpointTimer_Tick(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                TimeSpan currentRun = DateTime.Now - _currentStartTime;
                TimeSpan total = _accumulatedDayTotalBeforeCurrent + currentRun;
                _logManager.UpdateTempCheckpoint(_currentStartTime, currentRun, total);
            }
        }

        private void UpdateTrayTooltip()
        {
            TimeSpan currentRun = _isRecording ? (DateTime.Now - _currentStartTime) : TimeSpan.Zero;
            TimeSpan total = _accumulatedDayTotalBeforeCurrent + currentRun;

            string tooltip = TimeCalculator.FormatTrayTooltip(_currentRunNumber, currentRun, total, !_isRecording);
            Debug.WriteLine($"Updating tray tooltip: {tooltip}");
            if (tooltip.Length >= 64)
            {
                tooltip = tooltip.Substring(0, 63);
            }
            trayIcon.Text = tooltip;
        }

        private void BtnTotal_Click(object sender, EventArgs e)
        {
            RecalculateDayTotals();
            UpdateConsoleDisplay();
            MessageBox.Show($"Current Total for Today: {TimeCalculator.FormatHM(GetEffectiveTodayTotal(), true)}\r\n({TimeCalculator.FormatDecimalHours(GetEffectiveTodayTotal())})", "Today's Total Time", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReloadToday_Click(object sender, EventArgs e)
        {
            var todaySummary = _logManager.ReloadTodayRecordings();

          //  Console.WriteLine($"Reloaded today's totals: {TimeCalculator.FormatHM(todaySummary.TotalTime, true)} ({TimeCalculator.FormatDecimalHours(todaySummary.TotalTime)})");
            _accumulatedDayTotalBeforeCurrent = todaySummary.TotalTime;
            _currentRunNumber = todaySummary.Entries.Count + 1;

            UpdateConsoleDisplay();
            UpdateTrayTooltip();
            CheckMissingTimesAnomaly();

            ShowFeedback("Today's totals reloaded!");
        }

        private TimeSpan GetEffectiveTodayTotal()
        {
            TimeSpan activeRun = _isRecording ? (DateTime.Now - _currentStartTime) : TimeSpan.Zero;
            return _accumulatedDayTotalBeforeCurrent + activeRun;
        }

        private void UpdateConsoleDisplay()
        {
            TimeSpan currentRun = _isRecording ? (DateTime.Now - _currentStartTime) : TimeSpan.Zero;
            TimeSpan total = _accumulatedDayTotalBeforeCurrent + currentRun;

            string runLine = TimeCalculator.FormatRunLine(currentRun);
            string totalLine = TimeCalculator.FormatTotalLine(total);
            string startLine = _isRecording ? $"*Start Time: {_currentStartTime}" : "*Timer Idle / Paused";
            var todaySummary = _logManager.GetTodaySummary();
            rtbConsole.Text = $"{runLine}\r\n{totalLine}\r\n{startLine}\r\n";
            rtbConsole.AppendText($"\r\n");
            foreach (var entry in todaySummary.Entries)
            {
                var startStr = entry.StartTime.ToString("hh:mm:ss tt");
                var stopStr = entry.StopTime.Value.ToString("hh:mm:ss tt");
                rtbConsole.AppendText($"● Start={startStr}, Stop={stopStr}, Duration={TimeCalculator.FormatHM(entry.Duration, true)}\r\n");
            }
        }

        private string timeToCDT(string utc24HourString)
        {
DateTime utcDateTime = DateTime.ParseExact(
    utc24HourString, 
    "yyyy-MM-dd HH:mm:ss", 
    CultureInfo.InvariantCulture, 
    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
);

// 2. Find the Central Time zone (automatically handles CDT vs CST)
// "Central Standard Time" works on Windows and modern .NET (6+) on Linux/macOS
TimeZoneInfo centralZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

// 3. Convert to Central Time
DateTime centralTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, centralZone);

// Format as 12-hour with AM/PM or keep 24-hour
Console.WriteLine(centralTime.ToString("test 1: yyyy-MM-dd hh:mm:ss tt")); // 2026-07-15 01:30:00 PM (CDT)
Console.WriteLine(centralTime.ToString("test 2: yyyy-MM-dd HH:mm:ss"));    // 2026-07-15        }

return centralTime.ToString("hh:mm:ss tt");
        }


        private void BtnCopyTotal_Click(object sender, EventArgs e)
        {
            TimeSpan total = GetEffectiveTodayTotal();
            bool isHp = cbCopyFormat.SelectedIndex == 0;
            string textToCopy = isHp ? TimeCalculator.FormatHP(total, true) : TimeCalculator.FormatHM(total, false);

            Clipboard.SetText(textToCopy);
            ShowFeedback($"Copied: {textToCopy}");
        }

        private void ShowFeedback(string message)
        {
            lblCopyFeedback.Text = message;
            var timer = new System.Windows.Forms.Timer { Interval = 2500 };
            timer.Tick += (s, ev) => {
                lblCopyFeedback.Text = "";
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void BtnSetDirectory_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select Output Directory for Timer Logs",
                SelectedPath = _config.OutputDirectory
            };

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _config.OutputDirectory = fbd.SelectedPath;
                txtOutputDirectory.Text = _config.OutputDirectory;
                _configManager.SaveConfig(_config);

                _logManager.InitializeDirectory(_config.OutputDirectory, _config.SaveCustomLocation);
                RecalculateDayTotals();
                CheckMissingTimesAnomaly();
                UpdateConsoleDisplay();
            }
        }

        private void OpenAddModifyTime()
        {
            var form = new AddModifyTimeForm(_logManager, _isRecording, _currentStartTime, _config.Theme);
            form.OnCurrentRunModified += (newStart) => {
                _currentStartTime = newStart;
                UpdateTrayTooltip();
                UpdateConsoleDisplay();
            };
            form.OnTimesUpdated += () => {
                RecalculateDayTotals();
                CheckMissingTimesAnomaly();
                UpdateTrayTooltip();
                UpdateConsoleDisplay();
            };
            form.ShowDialog(this);
        }

        private void OpenLogViewer()
        {
            var form = new LogViewerSelectorForm(_logManager, _chromeActivityRecorder.ActivityLog, _config.Theme);
            form.ShowDialog(this);
        }

        private void OpenChromeActivity()
        {
            if (_chromeActivityForm == null || _chromeActivityForm.IsDisposed)
            {
                _chromeActivityForm = new ChromeActivityForm(_chromeActivityRecorder, _config.Theme);
            }

            if (!_chromeActivityForm.Visible)
            {
                _chromeActivityForm.Show(this);
            }
            else
            {
                _chromeActivityForm.WindowState = FormWindowState.Normal;
                _chromeActivityForm.BringToFront();
                _chromeActivityForm.Activate();
            }
        }

        private void OpenDbSyncComparison()
        {
            var form = new DbSyncComparisonForm(_config, _configManager, _logManager, _config.Theme);
            form.ShowDialog(this);
        }

        private void OpenSettings()
        {
            var form = new ThemeChooserForm(_config, _configManager, (newTheme) => {
                ThemeManager.ApplyTheme(this, newTheme);
            });
            form.ShowDialog(this);
        }

        private void OpenMySqlConfig()
        {
            var form = new MySqlConfigForm(_config, _configManager, _logManager, _config.Theme);
            form.ShowDialog(this);
        }

        private void ShowAboutLicense()
        {
            MessageBox.Show(
                "Time Recorder 2.0 (Timer 2.0)\r\n" +
                "Licensed under the GNU General Public License v3 (GPLv3).\r\n\r\n" +
                "This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.\r\n\r\n" +
                "Compatible with Windows 11 Desktop.",
                "About Time Recorder — GNU GPLv3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ImportOldLogs()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select time.txt or log.txt to Import",
                Filter = "Text Logs (*.txt)|*.txt|All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var parser = new LogParser();
                    var importedDays = parser.ParseTimeLogFile(ofd.FileName);

                    if (importedDays.Count == 0)
                    {
                        MessageBox.Show("No log entries could be identified in the selected file.", "Import Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Append imported file to current time.txt
                    string importedContent = File.ReadAllText(ofd.FileName);
                    File.AppendAllText(_logManager.TimeLogPath, "\r\n" + importedContent);

                    _logManager.InitializeDirectory(_logManager.LogDirectory, _config.SaveCustomLocation);
                    RecalculateDayTotals();
                    CheckMissingTimesAnomaly();
                    UpdateConsoleDisplay();

                    int warnings = importedDays.Sum(d => d.WarningCount);
                    string warningNotice = warnings > 0 ? $"\r\n\r\n⚠️ Note: {warnings} sessions in the imported log have missing stop times or anomalies." : "";

                    MessageBox.Show($"Successfully imported {importedDays.Count} day(s) from:\r\n{ofd.FileName}{warningNotice}", "Import Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import log file: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isExplicitExit && _config.CloseToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            if (_isRecording)
            {
                StopTimer();
            }

            UpdateConsoleDisplay();

            if (!_shutdownModalShown)
            {
                _shutdownModalShown = true;
                using (var modal = new ShutdownNotificationModal(
                    rtbConsole.Text,
                    _logManager.LogDirectory,
                    _logManager.TimeLogPath,
                    _config.Theme))
                {
                    modal.ShowDialog(this.Visible ? this : null);
                }
            }

            try
            {
                _chromeActivityRecorder.Dispose();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"Chrome activity recording could not be finalized:\r\n{ex.Message}",
                    "Chrome Activity Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            trayIcon.Visible = false;
        }

        private void CloseApplication()
        {
            _isExplicitExit = true;
            this.Close();
        }

        private class CopyFormatItem
        {
            public string Key { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display;
        }
    }

     
}
