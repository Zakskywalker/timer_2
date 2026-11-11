using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Database;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.App.Forms
{
    public class DbSyncComparisonForm : Form
    {
        private readonly AppConfig _config;
        private readonly ConfigManager _configManager;
        private readonly ILogManager _logManager;
        private readonly IDataConnector _dbConnector;
        private readonly string _themeName;

        private TabControl tabControl;
        private TabPage tabTimerLogs;
        private TabPage tabSystemLogs;

        // Timer tab controls
        private Label lblTimerTable;
        private TextBox txtTimerTable;
        private Button btnCompareTimer;
        private Button btnSubmitTimer;
        private Button btnRetrieveTimer;
        private ComboBox cbFilterStatus;
        private DataGridView dgvTimerCompare;
        private Label lblTimerStatus;

        // System tab controls
        private Label lblSystemTable;
        private TextBox txtSystemTable;
        private Button btnExportSystem;
        private Button btnRetrieveSystem;
        private DataGridView dgvSystemLogs;
        private Label lblSystemStatus;

        private DbComparisonResult _latestComparison;

        public DbSyncComparisonForm(AppConfig config, ConfigManager configManager, ILogManager logManager, string themeName)
        {
            _config = config;
            _configManager = configManager;
            _logManager = logManager;
            _themeName = themeName;
            _dbConnector = new MySqlDataConnector(_config.MySql);

            // Relative size matching the main timer form factor
            this.Text = "Database Sync, Compare & Export Manager";
            this.Size = new Size(580, 520);
            this.MinimumSize = new Size(500, 440);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _themeName);
        }

        private void InitializeComponent()
        {
            tabControl = new TabControl { Dock = DockStyle.Fill };

            tabTimerLogs = new TabPage("Timer Sessions Table");
            tabSystemLogs = new TabPage("System Logs Table");

            SetupTimerTab();
            SetupSystemTab();

            tabControl.TabPages.Add(tabTimerLogs);
            tabControl.TabPages.Add(tabSystemLogs);
            this.Controls.Add(tabControl);
        }

        private void SetupTimerTab()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 95, Padding = new Padding(10) };

            lblTimerTable = new Label { Text = "Table Name:", Location = new Point(10, 12), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtTimerTable = new TextBox { Text = _config.MySql.TimerLogsTable, Location = new Point(95, 9), Width = 150 };
            txtTimerTable.TextChanged += (s, e) => {
                _config.MySql.TimerLogsTable = txtTimerTable.Text.Trim();
                _configManager.SaveConfig(_config);
            };

            var lblFilter = new Label { Text = "Filter:", Location = new Point(265, 12), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cbFilterStatus = new ComboBox { Location = new Point(310, 9), Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            cbFilterStatus.Items.AddRange(new object[] { "Show All", "To Upload", "Modified", "In Sync", "In DB Only" });
            cbFilterStatus.SelectedIndex = 0;
            cbFilterStatus.SelectedIndexChanged += (s, e) => ApplyGridFilter();

            btnCompareTimer = new Button { Text = "1. Compare with DB", Location = new Point(10, 42), Width = 150, Height = 34, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnCompareTimer.Click += async (s, e) => await CompareWithDbAsync();

            btnSubmitTimer = new Button { Text = "2. User Submit to DB", Location = new Point(170, 42), Width = 155, Height = 34, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnSubmitTimer.Click += async (s, e) => await SubmitSelectedToDbAsync();

            btnRetrieveTimer = new Button { Text = "Retrieve DB Raw", Location = new Point(335, 42), Width = 130, Height = 34 };
            btnRetrieveTimer.Click += async (s, e) => await RetrieveDbRawAsync();

            topPanel.Controls.AddRange(new Control[] { lblTimerTable, txtTimerTable, lblFilter, cbFilterStatus, btnCompareTimer, btnSubmitTimer, btnRetrieveTimer });
            tabTimerLogs.Controls.Add(topPanel);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(8, 4, 8, 4) };
            lblTimerStatus = new Label { Dock = DockStyle.Fill, Text = "Ready. Click 'Compare with DB' to inspect sync differences.", Font = new Font("Segoe UI", 8.5f) };
            bottomPanel.Controls.Add(lblTimerStatus);
            tabTimerLogs.Controls.Add(bottomPanel);

            dgvTimerCompare = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(20, 20, 20)
            };

            var chkCol = new DataGridViewCheckBoxColumn { HeaderText = "Submit?", Width = 55, Name = "colSelect" };
            dgvTimerCompare.Columns.Add(chkCol);
            dgvTimerCompare.Columns.Add("colDate", "Date");
            dgvTimerCompare.Columns.Add("colRun", "Run");
            dgvTimerCompare.Columns.Add("colLocal", "Local Time (Duration)");
            dgvTimerCompare.Columns.Add("colDb", "DB Time (Duration)");
            dgvTimerCompare.Columns.Add("colStatus", "Status");

            tabTimerLogs.Controls.Add(dgvTimerCompare);
            dgvTimerCompare.BringToFront();
        }

        private void SetupSystemTab()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };

            lblSystemTable = new Label { Text = "System Table:", Location = new Point(10, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtSystemTable = new TextBox { Text = _config.MySql.SystemLogsTable, Location = new Point(110, 13), Width = 160 };
            txtSystemTable.TextChanged += (s, e) => {
                _config.MySql.SystemLogsTable = txtSystemTable.Text.Trim();
                _configManager.SaveConfig(_config);
            };

            btnExportSystem = new Button { Text = "Export System Logs to DB", Location = new Point(285, 11), Width = 180, Height = 32, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnExportSystem.Click += async (s, e) => await ExportSystemLogsAsync();

            btnRetrieveSystem = new Button { Text = "Retrieve Logs", Location = new Point(475, 11), Width = 110, Height = 32 };
            btnRetrieveSystem.Click += async (s, e) => await RetrieveSystemLogsAsync();

            topPanel.Controls.AddRange(new Control[] { lblSystemTable, txtSystemTable, btnExportSystem, btnRetrieveSystem });
            tabSystemLogs.Controls.Add(topPanel);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(8, 4, 8, 4) };
            lblSystemStatus = new Label { Dock = DockStyle.Fill, Text = "Ready to export or retrieve system event logs.", Font = new Font("Segoe UI", 8.5f) };
            bottomPanel.Controls.Add(lblSystemStatus);
            tabSystemLogs.Controls.Add(bottomPanel);

            dgvSystemLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(20, 20, 20)
            };
            dgvSystemLogs.Columns.Add("colId", "ID");
            dgvSystemLogs.Columns.Add("colTime", "Timestamp");
            dgvSystemLogs.Columns.Add("colLevel", "Level");
            dgvSystemLogs.Columns.Add("colMsg", "Message");

            tabSystemLogs.Controls.Add(dgvSystemLogs);
            dgvSystemLogs.BringToFront();
        }

        private async Task CompareWithDbAsync()
        {
            lblTimerStatus.Text = "Connecting and comparing local records against database table...";
            btnCompareTimer.Enabled = false;

            var localDays = _logManager.GetAllDays();
            var (result, message) = await _dbConnector.CompareTimerLogsAsync(localDays, txtTimerTable.Text.Trim());

            _latestComparison = result;
            lblTimerStatus.Text = message;
            btnCompareTimer.Enabled = true;

            ApplyGridFilter();
        }

        private void ApplyGridFilter()
        {
            dgvTimerCompare.Rows.Clear();
            if (_latestComparison == null) return;

            string filter = cbFilterStatus.SelectedItem?.ToString() ?? "Show All";

            foreach (var item in _latestComparison.Items)
            {
                if (filter == "To Upload" && item.Status != DbSyncStatus.ToUpload) continue;
                if (filter == "Modified" && item.Status != DbSyncStatus.Modified) continue;
                if (filter == "In Sync" && item.Status != DbSyncStatus.InSync) continue;
                if (filter == "In DB Only" && item.Status != DbSyncStatus.InDbOnly) continue;

                string localStr = $"{item.LocalStart:hh:mm tt} - {(item.LocalStop.HasValue ? item.LocalStop.Value.ToString("hh:mm tt") : "Running")} ({(int)item.LocalDuration.TotalMinutes}m)";
                string dbStr = item.DbStart.HasValue 
                    ? $"{item.DbStart.Value:hh:mm tt} - {(item.DbStop.HasValue ? item.DbStop.Value.ToString("hh:mm tt") : "Running")} ({(int)item.DbDuration.TotalMinutes}m)"
                    : "Not in DB";

                bool defaultChecked = (item.Status == DbSyncStatus.ToUpload || item.Status == DbSyncStatus.Modified);

                int rowIdx = dgvTimerCompare.Rows.Add(
                    defaultChecked,
                    item.Date.ToString("MM/dd/yyyy"),
                    $"Run {item.RunNumber}",
                    localStr,
                    dbStr,
                    item.StatusDescription
                );

                var row = dgvTimerCompare.Rows[rowIdx];
                row.Tag = item;

                if (item.Status == DbSyncStatus.ToUpload)
                {
                    row.Cells["colStatus"].Style.ForeColor = Color.LimeGreen;
                }
                else if (item.Status == DbSyncStatus.Modified)
                {
                    row.Cells["colStatus"].Style.ForeColor = Color.Gold;
                }
                else if (item.Status == DbSyncStatus.InDbOnly)
                {
                    row.Cells["colStatus"].Style.ForeColor = Color.CornflowerBlue;
                }
                else
                {
                    row.Cells["colStatus"].Style.ForeColor = Color.LightGray;
                }
            }
        }

        private async Task SubmitSelectedToDbAsync()
        {
            var selectedSessions = new List<TimeEntry>();
            var allLocal = _logManager.GetAllDays().SelectMany(d => d.Entries).ToDictionary(e => e.Id, e => e);

            foreach (DataGridViewRow row in dgvTimerCompare.Rows)
            {
                bool isChecked = Convert.ToBoolean(row.Cells["colSelect"].Value);
                if (isChecked && row.Tag is DbTimerComparisonItem item)
                {
                    if (allLocal.TryGetValue(item.SessionId, out var entry))
                    {
                        selectedSessions.Add(entry);
                    }
                }
            }

            if (selectedSessions.Count == 0)
            {
                MessageBox.Show("No records selected for database submission. Please check the rows you wish to submit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Submit {selectedSessions.Count} selected timer session(s) into MySQL table `{txtTimerTable.Text.Trim()}`?",
                "Confirm User Submit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                lblTimerStatus.Text = "Submitting selected records to database...";
                btnSubmitTimer.Enabled = false;

                var (count, msg) = await _dbConnector.ExportTimerLogsToDbAsync(selectedSessions, txtTimerTable.Text.Trim());
                lblTimerStatus.Text = msg;
                btnSubmitTimer.Enabled = true;

                MessageBox.Show(msg, "Submission Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CompareWithDbAsync();
            }
        }

        private async Task RetrieveDbRawAsync()
        {
            lblTimerStatus.Text = "Retrieving records from database table...";
            var entries = await _dbConnector.RetrieveTimerLogsFromDbAsync(txtTimerTable.Text.Trim());

            dgvTimerCompare.Rows.Clear();
            foreach (var e in entries)
            {
                string stop = e.StopTime.HasValue ? e.StopTime.Value.ToString("hh:mm tt") : "Running";
                dgvTimerCompare.Rows.Add(false, e.Date.ToString("MM/dd/yyyy"), $"Run {e.RunNumber}", "-", $"{e.StartTime:hh:mm tt} - {stop} ({(int)e.Duration.TotalMinutes}m)", "Retrieved from DB");
            }
            lblTimerStatus.Text = $"Retrieved {entries.Count} record(s) from table `{txtTimerTable.Text.Trim()}`.";
        }

        private async Task ExportSystemLogsAsync()
        {
            if (!File.Exists(_logManager.SystemLogPath))
            {
                MessageBox.Show("System log file not found.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lines = File.ReadAllLines(_logManager.SystemLogPath).ToList();
            lblSystemStatus.Text = "Exporting system log events to database...";
            btnExportSystem.Enabled = false;

            var (count, msg) = await _dbConnector.ExportSystemLogsToDbAsync(lines, txtSystemTable.Text.Trim());
            lblSystemStatus.Text = msg;
            btnExportSystem.Enabled = true;

            MessageBox.Show(msg, "System Logs Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RetrieveSystemLogsAsync();
        }

        private async Task RetrieveSystemLogsAsync()
        {
            lblSystemStatus.Text = "Querying system logs from database...";
            var items = await _dbConnector.RetrieveSystemLogsFromDbAsync(txtSystemTable.Text.Trim());

            dgvSystemLogs.Rows.Clear();
            foreach (var item in items)
            {
                dgvSystemLogs.Rows.Add(item.Id, item.LogTime.ToString("MM/dd/yyyy HH:mm:ss"), item.Level, item.Message);
            }
            lblSystemStatus.Text = $"Loaded {items.Count} system log event(s) from `{txtSystemTable.Text.Trim()}`.";
        }
    }
}

