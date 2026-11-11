using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Calculations;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.App.Forms
{
    public class AddModifyTimeForm : Form
    {
        private readonly ILogManager _logManager;
        private readonly bool _isCurrentlyRecording;
        private readonly DateTime _currentRunStart;
        private readonly string _themeName;

        public event Action<DateTime> OnCurrentRunModified;
        public event Action OnTimesUpdated;

        private RadioButton rbModifyCurrent;
        private RadioButton rbAddNew;
        private RadioButton rbModifyExisting;

        private Panel panelCurrent;
        private Panel panelAddNew;
        private Panel panelModifyExisting;

        // Modify Current controls
        private DateTimePicker dtpCurrentStart;
        private Label lblCurrentOriginal;
        private Button btnApplyCurrent;

        // Add New controls
        private DateTimePicker dtpNewStartDate;
        private DateTimePicker dtpNewStartTime;
        private DateTimePicker dtpNewEndDate;
        private DateTimePicker dtpNewEndTime;
        private TextBox txtNewNote;
        private Label lblNewPreview;
        private Button btnSaveNew;

        // Modify Existing controls
        private ComboBox cbExistingDates;
        private ComboBox cbExistingSessions;
        private DateTimePicker dtpExistingStartDate;
        private DateTimePicker dtpExistingStartTime;
        private DateTimePicker dtpExistingEndDate;
        private DateTimePicker dtpExistingEndTime;
        private TextBox txtExistingNote;
        private Label lblExistingPreview;
        private Button btnSaveExisting;

        public AddModifyTimeForm(ILogManager logManager, bool isCurrentlyRecording, DateTime currentRunStart, string themeName)
        {
            _logManager = logManager;
            _isCurrentlyRecording = isCurrentlyRecording;
            _currentRunStart = currentRunStart;
            _themeName = themeName;

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _themeName);
        }

        private void InitializeComponent()
        {
            this.Text = "Add / Modify Time Period";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15, 10, 15, 10) };
            
            rbModifyCurrent = new RadioButton
            {
                Text = "Modify Current Run",
                Location = new Point(15, 18),
                AutoSize = true,
                Enabled = _isCurrentlyRecording,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            rbAddNew = new RadioButton
            {
                Text = "Add New Time Period",
                Location = new Point(190, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            rbModifyExisting = new RadioButton
            {
                Text = "Modify Existing Period",
                Location = new Point(370, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            topPanel.Controls.Add(rbModifyCurrent);
            topPanel.Controls.Add(rbAddNew);
            topPanel.Controls.Add(rbModifyExisting);
            this.Controls.Add(topPanel);

            // Container for panels
            var contentContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(contentContainer);

            // Panel 1: Modify Current
            panelCurrent = new Panel { Dock = DockStyle.Fill, Visible = false };
            SetupModifyCurrentPanel();
            contentContainer.Controls.Add(panelCurrent);

            // Panel 2: Add New Period
            panelAddNew = new Panel { Dock = DockStyle.Fill, Visible = false };
            SetupAddNewPanel();
            contentContainer.Controls.Add(panelAddNew);

            // Panel 3: Modify Existing
            panelModifyExisting = new Panel { Dock = DockStyle.Fill, Visible = false };
            SetupModifyExistingPanel();
            contentContainer.Controls.Add(panelModifyExisting);

            // Wire mode switching
            rbModifyCurrent.CheckedChanged += (s, e) => SwitchMode();
            rbAddNew.CheckedChanged += (s, e) => SwitchMode();
            rbModifyExisting.CheckedChanged += (s, e) => SwitchMode();

            if (_isCurrentlyRecording)
            {
                rbModifyCurrent.Checked = true;
            }
            else
            {
                rbAddNew.Checked = true;
            }
        }

        private void SwitchMode()
        {
            panelCurrent.Visible = rbModifyCurrent.Checked;
            panelAddNew.Visible = rbAddNew.Checked;
            panelModifyExisting.Visible = rbModifyExisting.Checked;

            if (rbModifyExisting.Checked)
            {
                PopulateExistingDatesDropdown();
            }
        }

        private void SetupModifyCurrentPanel()
        {
            var lblTitle = new Label
            {
                Text = "Adjust Start Time for Current Active Run:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(10, 15),
                AutoSize = true
            };

            lblCurrentOriginal = new Label
            {
                Text = $"Current Recorded Start: {_currentRunStart:MM/dd/yyyy hh:mm:ss tt}",
                Location = new Point(10, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblPrompt = new Label
            {
                Text = "New Start Time:",
                Location = new Point(10, 95),
                AutoSize = true
            };

            dtpCurrentStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = _isCurrentlyRecording ? _currentRunStart : DateTime.Now,
                Location = new Point(130, 92),
                Width = 140
            };

            var lblNotice = new Label
            {
                Text = "Note: Modifying the active start time will immediately adjust the running duration, notifications, and today's total.",
                Location = new Point(10, 140),
                Size = new Size(480, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };

            btnApplyCurrent = new Button
            {
                Text = "Accept & Update Start Time",
                Location = new Point(10, 210),
                Size = new Size(240, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnApplyCurrent.Click += BtnApplyCurrent_Click;

            panelCurrent.Controls.Add(lblTitle);
            panelCurrent.Controls.Add(lblCurrentOriginal);
            panelCurrent.Controls.Add(lblPrompt);
            panelCurrent.Controls.Add(dtpCurrentStart);
            panelCurrent.Controls.Add(lblNotice);
            panelCurrent.Controls.Add(btnApplyCurrent);
        }

        private void BtnApplyCurrent_Click(object sender, EventArgs e)
        {
            DateTime newStartTime = new DateTime(
                _currentRunStart.Year, _currentRunStart.Month, _currentRunStart.Day,
                dtpCurrentStart.Value.Hour, dtpCurrentStart.Value.Minute, dtpCurrentStart.Value.Second);

            if (newStartTime > DateTime.Now)
            {
                MessageBox.Show("Start time cannot be set to the future.", "Invalid Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Original Start Time: {_currentRunStart:T}\r\nNew Start Time: {newStartTime:T}\r\n\r\nAre you sure you want to update the start time for the active session?",
                "Confirm Start Time Adjustment",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                TimeSpan newRun = DateTime.Now - newStartTime;
                TimeSpan todayTotal = _logManager.GetTodaySummary().TotalTime + newRun;
                _logManager.ModifyCurrentRunStart(_currentRunStart, newStartTime, newRun, todayTotal);

                OnCurrentRunModified?.Invoke(newStartTime);
                OnTimesUpdated?.Invoke();

                MessageBox.Show("Start time updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void SetupAddNewPanel()
        {
            int y = 15;

            var lblTitle = new Label
            {
                Text = "Enter New Time Period to Add:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(10, y),
                AutoSize = true
            };
            y += 35;

            // Start Date & Time
            var lblStart = new Label { Text = "Start Date & Time:", Location = new Point(10, y + 4), AutoSize = true };
            dtpNewStartDate = new DateTimePicker { Location = new Point(150, y), Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpNewStartTime = new DateTimePicker { Location = new Point(300, y), Width = 110, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Now.AddHours(-1) };
            panelAddNew.Controls.AddRange(new Control[] { lblTitle, lblStart, dtpNewStartDate, dtpNewStartTime });
            y += 40;

            // End Date & Time
            var lblEnd = new Label { Text = "End Date & Time:", Location = new Point(10, y + 4), AutoSize = true };
            dtpNewEndDate = new DateTimePicker { Location = new Point(150, y), Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpNewEndTime = new DateTimePicker { Location = new Point(300, y), Width = 110, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Now };
            panelAddNew.Controls.AddRange(new Control[] { lblEnd, dtpNewEndDate, dtpNewEndTime });
            y += 40;

            // Notes
            var lblNote = new Label { Text = "Description / Reason:", Location = new Point(10, y + 4), AutoSize = true };
            txtNewNote = new TextBox { Location = new Point(150, y), Width = 320, Text = "Manual time entry" };
            panelAddNew.Controls.AddRange(new Control[] { lblNote, txtNewNote });
            y += 40;

            lblNewPreview = new Label
            {
                Text = "Calculated Duration: 1 Hrs. 0 Min. (1.00 hrs)",
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            panelAddNew.Controls.Add(lblNewPreview);
            y += 40;

            btnSaveNew = new Button
            {
                Text = "Save & Confirm Period",
                Location = new Point(10, y),
                Size = new Size(200, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSaveNew.Click += BtnSaveNew_Click;
            panelAddNew.Controls.Add(btnSaveNew);

            // Live preview update
            dtpNewStartDate.ValueChanged += (s, e) => UpdateNewPreview();
            dtpNewStartTime.ValueChanged += (s, e) => UpdateNewPreview();
            dtpNewEndDate.ValueChanged += (s, e) => UpdateNewPreview();
            dtpNewEndTime.ValueChanged += (s, e) => UpdateNewPreview();
            UpdateNewPreview();
        }

        private void UpdateNewPreview()
        {
            var start = dtpNewStartDate.Value.Date + dtpNewStartTime.Value.TimeOfDay;
            var end = dtpNewEndDate.Value.Date + dtpNewEndTime.Value.TimeOfDay;
            var dur = end - start;

            if (dur <= TimeSpan.Zero)
            {
                lblNewPreview.Text = "⚠️ End time must be after Start time.";
                lblNewPreview.ForeColor = Color.Red;
            }
            else
            {
                lblNewPreview.Text = $"Calculated Duration: {TimeCalculator.FormatHM(dur, true)} ({TimeCalculator.FormatDecimalHours(dur)})";
                lblNewPreview.ForeColor = ThemeManager.GetPalette(_themeName).AccentLabelColor;
            }
        }

        private void BtnSaveNew_Click(object sender, EventArgs e)
        {
            var start = dtpNewStartDate.Value.Date + dtpNewStartTime.Value.TimeOfDay;
            var end = dtpNewEndDate.Value.Date + dtpNewEndTime.Value.TimeOfDay;

            if (start >= end)
            {
                MessageBox.Show("Start time must be strictly before End time.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dur = end - start;
            var confirm = MessageBox.Show(
                $"Add new period:\r\nStart: {start:g}\r\nEnd:   {end:g}\r\nDuration: {TimeCalculator.FormatHM(dur, true)} ({TimeCalculator.FormatDecimalHours(dur)})\r\n\r\nConfirm saving this time entry?",
                "Confirm New Time Period",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _logManager.AddManualTimePeriod(start, end, txtNewNote.Text, out _, out var newTotal);
                OnTimesUpdated?.Invoke();

                MessageBox.Show($"New period saved successfully!\r\nUpdated day total: {TimeCalculator.FormatHM(newTotal, true)}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void SetupModifyExistingPanel()
        {
            int y = 15;

            var lblTitle = new Label
            {
                Text = "Select and Modify an Existing Recorded Session:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(10, y),
                AutoSize = true
            };
            y += 35;

            // Date picker dropdown
            var lblSelectDate = new Label { Text = "Select Date:", Location = new Point(10, y + 4), AutoSize = true };
            cbExistingDates = new ComboBox { Location = new Point(150, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cbExistingDates.SelectedIndexChanged += CbExistingDates_SelectedIndexChanged;
            panelModifyExisting.Controls.AddRange(new Control[] { lblTitle, lblSelectDate, cbExistingDates });
            y += 40;

            // Session picker dropdown
            var lblSelectSession = new Label { Text = "Select Session:", Location = new Point(10, y + 4), AutoSize = true };
            cbExistingSessions = new ComboBox { Location = new Point(150, y), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            cbExistingSessions.SelectedIndexChanged += CbExistingSessions_SelectedIndexChanged;
            panelModifyExisting.Controls.AddRange(new Control[] { lblSelectSession, cbExistingSessions });
            y += 40;

            // Edit Start Date & Time
            var lblStart = new Label { Text = "Start Date & Time:", Location = new Point(10, y + 4), AutoSize = true };
            dtpExistingStartDate = new DateTimePicker { Location = new Point(150, y), Width = 140, Format = DateTimePickerFormat.Short };
            dtpExistingStartTime = new DateTimePicker { Location = new Point(300, y), Width = 110, Format = DateTimePickerFormat.Time, ShowUpDown = true };
            panelModifyExisting.Controls.AddRange(new Control[] { lblStart, dtpExistingStartDate, dtpExistingStartTime });
            y += 40;

            // Edit End Date & Time
            var lblEnd = new Label { Text = "End Date & Time:", Location = new Point(10, y + 4), AutoSize = true };
            dtpExistingEndDate = new DateTimePicker { Location = new Point(150, y), Width = 140, Format = DateTimePickerFormat.Short };
            dtpExistingEndTime = new DateTimePicker { Location = new Point(300, y), Width = 110, Format = DateTimePickerFormat.Time, ShowUpDown = true };
            panelModifyExisting.Controls.AddRange(new Control[] { lblEnd, dtpExistingEndDate, dtpExistingEndTime });
            y += 40;

            // Notes
            var lblNote = new Label { Text = "Reason for Change:", Location = new Point(10, y + 4), AutoSize = true };
            txtExistingNote = new TextBox { Location = new Point(150, y), Width = 320, Text = "Corrected logged times" };
            panelModifyExisting.Controls.AddRange(new Control[] { lblNote, txtExistingNote });
            y += 40;

            lblExistingPreview = new Label
            {
                Text = "Duration: -",
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            panelModifyExisting.Controls.Add(lblExistingPreview);
            y += 40;

            btnSaveExisting = new Button
            {
                Text = "Apply Modification & Update Logs",
                Location = new Point(10, y),
                Size = new Size(260, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSaveExisting.Click += BtnSaveExisting_Click;
            panelModifyExisting.Controls.Add(btnSaveExisting);

            // Wire previews
            dtpExistingStartDate.ValueChanged += (s, e) => UpdateExistingPreview();
            dtpExistingStartTime.ValueChanged += (s, e) => UpdateExistingPreview();
            dtpExistingEndDate.ValueChanged += (s, e) => UpdateExistingPreview();
            dtpExistingEndTime.ValueChanged += (s, e) => UpdateExistingPreview();
        }

        private void PopulateExistingDatesDropdown()
        {
            cbExistingDates.Items.Clear();
            var days = _logManager.GetAllDays().OrderByDescending(d => d.Date).ToList();

            foreach (var d in days)
            {
                cbExistingDates.Items.Add(d.Date.ToString("MM/dd/yyyy"));
            }

            if (cbExistingDates.Items.Count > 0)
            {
                cbExistingDates.SelectedIndex = 0;
            }
        }

        private void CbExistingDates_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbExistingSessions.Items.Clear();
            if (cbExistingDates.SelectedItem == null) return;

            string dateStr = cbExistingDates.SelectedItem.ToString();
            if (DateTime.TryParse(dateStr, out var selectedDate))
            {
                var day = _logManager.GetAllDays().FirstOrDefault(d => d.Date == selectedDate.Date);
                if (day != null)
                {
                    for (int i = 0; i < day.Entries.Count; i++)
                    {
                        var entry = day.Entries[i];
                        string stopStr = entry.StopTime.HasValue ? entry.StopTime.Value.ToString("hh:mm:ss tt") : "Incomplete";
                        string itemText = $"Run {entry.RunNumber}: [{entry.StartTime:hh:mm:ss tt} - {stopStr}] ({TimeCalculator.FormatDecimalHours(entry.Duration)})";
                        cbExistingSessions.Items.Add(new ComboBoxItem { Text = itemText, Value = i });
                    }
                    if (cbExistingSessions.Items.Count > 0)
                    {
                        cbExistingSessions.SelectedIndex = 0;
                    }
                }
            }
        }

        private void CbExistingSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbExistingSessions.SelectedItem is ComboBoxItem item && cbExistingDates.SelectedItem != null)
            {
                string dateStr = cbExistingDates.SelectedItem.ToString();
                if (DateTime.TryParse(dateStr, out var selectedDate))
                {
                    var day = _logManager.GetAllDays().FirstOrDefault(d => d.Date == selectedDate.Date);
                    int idx = (int)item.Value;
                    if (day != null && idx >= 0 && idx < day.Entries.Count)
                    {
                        var entry = day.Entries[idx];
                        dtpExistingStartDate.Value = entry.StartTime.Date;
                        dtpExistingStartTime.Value = entry.StartTime;

                        var stop = entry.StopTime ?? entry.StartTime.AddMinutes(30);
                        dtpExistingEndDate.Value = stop.Date;
                        dtpExistingEndTime.Value = stop;

                        txtExistingNote.Text = string.IsNullOrEmpty(entry.Notes) ? "Corrected time" : entry.Notes;
                        UpdateExistingPreview();
                    }
                }
            }
        }

        private void UpdateExistingPreview()
        {
            var start = dtpExistingStartDate.Value.Date + dtpExistingStartTime.Value.TimeOfDay;
            var end = dtpExistingEndDate.Value.Date + dtpExistingEndTime.Value.TimeOfDay;
            var dur = end - start;

            if (dur <= TimeSpan.Zero)
            {
                lblExistingPreview.Text = "⚠️ End time must be after Start time.";
                lblExistingPreview.ForeColor = Color.Red;
            }
            else
            {
                lblExistingPreview.Text = $"Duration: {TimeCalculator.FormatHM(dur, true)} ({TimeCalculator.FormatDecimalHours(dur)})";
                lblExistingPreview.ForeColor = ThemeManager.GetPalette(_themeName).AccentLabelColor;
            }
        }

        private void BtnSaveExisting_Click(object sender, EventArgs e)
        {
            if (!(cbExistingSessions.SelectedItem is ComboBoxItem item) || cbExistingDates.SelectedItem == null)
            {
                MessageBox.Show("Please select a session to modify.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var start = dtpExistingStartDate.Value.Date + dtpExistingStartTime.Value.TimeOfDay;
            var end = dtpExistingEndDate.Value.Date + dtpExistingEndTime.Value.TimeOfDay;

            if (start >= end)
            {
                MessageBox.Show("Start time must be strictly before End time.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dateStr = cbExistingDates.SelectedItem.ToString();
            DateTime.TryParse(dateStr, out var selectedDate);
            int sessionIndex = (int)item.Value;

            var confirm = MessageBox.Show(
                $"Update Session #{sessionIndex + 1} on {selectedDate:MM/dd/yyyy}:\r\nStart: {start:T}\r\nEnd:   {end:T}\r\n\r\nConfirm modifying this log record?",
                "Confirm Modification",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _logManager.ModifyExistingTimePeriod(selectedDate, sessionIndex, start, end, txtExistingNote.Text, out var newTotal);
                OnTimesUpdated?.Invoke();

                MessageBox.Show($"Session updated!\r\nUpdated day total: {TimeCalculator.FormatHM(newTotal, true)}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private class ComboBoxItem
        {
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString() => Text;
        }
    }
}
