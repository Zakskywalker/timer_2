using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Timer2.App.Services;
using Timer2.App.UI;
using Timer2.Core.Models;

namespace Timer2.App.Forms
{
    public sealed class ChromeActivityForm : Form
    {
        private readonly ChromeActivityRecorder _recorder;
        private readonly ThemePalette _palette;
        private readonly ComboBox _dayPicker;
        private readonly Button _recordButton;
        private readonly Button _captureButton;
        private readonly Label _statusLabel;
        private readonly RichTextBox _entriesView;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private DateTime? _displayedDay;
        private string _displayedPath;
        private int _displayedEntryCount;

        public ChromeActivityForm(ChromeActivityRecorder recorder, string themeName)
        {
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            _palette = ThemeManager.GetPalette(themeName);

            Text = "Google Chrome Activity";
            Size = new Size(860, 560);
            MinimumSize = new Size(650, 400);
            StartPosition = FormStartPosition.CenterParent;

            var controlsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96
            };

            _recordButton = new Button
            {
                Location = new Point(14, 12),
                Size = new Size(132, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _recordButton.Click += RecordButton_Click;

            _captureButton = new Button
            {
                Text = "Capture Current Tab",
                Location = new Point(154, 12),
                Size = new Size(152, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _captureButton.Click += CaptureButton_Click;

            var dayLabel = new Label
            {
                Text = "Log day:",
                Location = new Point(324, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            _dayPicker = new ComboBox
            {
                Location = new Point(384, 15),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _dayPicker.SelectedIndexChanged += (sender, args) =>
            {
                _displayedDay = null;
                RefreshEntries();
            };

            var captureNote = new Label
            {
                Text = "Process-only: records Chrome's foreground tab/window title. URLs and background-tab changes are not available.",
                Location = new Point(14, 54),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };

            controlsPanel.Controls.AddRange(new Control[] { _recordButton, _captureButton, dayLabel, _dayPicker, captureNote });

            _entriesView = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                Font = new Font("Consolas", 9f),
                WordWrap = false
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Padding = new Padding(10, 5, 10, 0),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            Controls.Add(_entriesView);
            Controls.Add(_statusLabel);
            Controls.Add(controlsPanel);

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            FormClosing += (sender, args) => _refreshTimer.Stop();

            ThemeManager.ApplyTheme(this, themeName);
            RefreshDays(DateTime.Today);
            UpdateRecordingControls();
            RefreshEntries();
            _refreshTimer.Start();
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_recorder.IsRecording)
                {
                    _recorder.Stop();
                    _statusLabel.Text = "Chrome activity recording stopped.";
                }
                else
                {
                    _recorder.Start();
                    _dayPicker.SelectedIndex = -1;
                    RefreshDays(DateTime.Today);
                    _statusLabel.Text = "Recording foreground Chrome title changes. The timer continues independently.";
                }

                UpdateRecordingControls();
                RefreshEntries();
            }
            catch (Exception ex) when (IsExpectedRecorderException(ex))
            {
                ShowRecorderError(ex);
            }
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_recorder.CaptureCurrentTab())
                {
                    _statusLabel.Text = "No Chrome window was found. Open Chrome and try again.";
                    return;
                }

                _dayPicker.SelectedIndex = -1;
                RefreshDays(DateTime.Today);
                _statusLabel.Text = "Captured the current Chrome window title.";
                RefreshEntries();
            }
            catch (Exception ex) when (IsExpectedRecorderException(ex))
            {
                ShowRecorderError(ex);
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            UpdateRecordingControls();
            if (_recorder.IsRecording &&
                _dayPicker.SelectedItem is DayOption displayed &&
                displayed.Date != DateTime.Today)
            {
                _dayPicker.SelectedIndex = -1;
                RefreshDays(DateTime.Today);
            }

            if (!string.IsNullOrEmpty(_recorder.LastError))
            {
                _statusLabel.Text = $"Chrome activity logging error: {_recorder.LastError}";
                return;
            }

            if (_statusLabel.Text.StartsWith("Chrome activity logging error:", StringComparison.Ordinal))
            {
                _statusLabel.Text = _recorder.IsRecording
                    ? "Recording foreground Chrome title changes. The timer continues independently."
                    : "Chrome activity recording is stopped.";
            }
            RefreshEntries();
        }

        private void UpdateRecordingControls()
        {
            bool recording = _recorder.IsRecording;
            _recordButton.Text = recording ? "Stop Recording" : "Start Recording";
            _dayPicker.Enabled = !recording;
            _recordButton.BackColor = recording ? Color.Firebrick : _palette.ButtonBackground;
            _recordButton.ForeColor = recording ? Color.White : _palette.ButtonForeground;
        }

        private void RefreshDays(DateTime preferredDay)
        {
            DateTime? selectedDay = _dayPicker.SelectedItem is DayOption existing
                ? existing.Date
                : (DateTime?)null;

            var days = _recorder.ActivityLog.GetAvailableDays();
            if (!days.Contains(preferredDay.Date))
            {
                days.Insert(0, preferredDay.Date);
            }

            _dayPicker.BeginUpdate();
            _dayPicker.Items.Clear();
            foreach (DateTime day in days.OrderByDescending(date => date))
            {
                _dayPicker.Items.Add(new DayOption(day));
            }

            DateTime target = selectedDay.HasValue && days.Contains(selectedDay.Value)
                ? selectedDay.Value
                : preferredDay.Date;
            for (int i = 0; i < _dayPicker.Items.Count; i++)
            {
                if (((DayOption)_dayPicker.Items[i]).Date == target)
                {
                    _dayPicker.SelectedIndex = i;
                    break;
                }
            }
            _dayPicker.EndUpdate();
        }

        private void RefreshEntries()
        {
            if (_dayPicker.SelectedItem is not DayOption selected)
            {
                return;
            }

            try
            {
                var entries = _recorder.ActivityLog.GetEntriesForDay(selected.Date);
                string path = _recorder.ActivityLog.GetPathForDay(selected.Date);
                bool rebuild = !_displayedDay.HasValue ||
                    _displayedDay.Value != selected.Date ||
                    !string.Equals(_displayedPath, path, StringComparison.OrdinalIgnoreCase) ||
                    entries.Count < _displayedEntryCount;
                int firstNewEntry = rebuild ? 0 : _displayedEntryCount;

                if (rebuild)
                {
                    _entriesView.Clear();
                }

                _entriesView.SuspendLayout();
                for (int i = firstNewEntry; i < entries.Count; i++)
                {
                    ChromeActivityEntry entry = entries[i];
                    _entriesView.AppendText(
                        $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}  {FormatEvent(entry.EventType),-24}  {entry.WindowTitle}{Environment.NewLine}");
                }
                _entriesView.SelectionStart = _entriesView.TextLength;
                _entriesView.ScrollToCaret();
                _entriesView.ResumeLayout();
                _displayedDay = selected.Date;
                _displayedPath = path;
                _displayedEntryCount = entries.Count;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is System.Text.Json.JsonException)
            {
                _statusLabel.Text = $"Chrome activity log could not be read: {ex.Message}";
            }
        }

        private void ShowRecorderError(Exception exception)
        {
            _statusLabel.Text = $"Chrome activity logging error: {exception.Message}";
            MessageBox.Show(
                this,
                $"Chrome activity could not be recorded:\r\n{exception.Message}",
                "Chrome Activity Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool IsExpectedRecorderException(Exception exception) =>
            exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is Win32Exception ||
            exception is InvalidOperationException ||
            exception is ArgumentException;

        private static string FormatEvent(string eventType) =>
            eventType switch
            {
                ChromeActivityEventTypes.RecordingStarted => "Recording started",
                ChromeActivityEventTypes.RecordingStopped => "Recording stopped",
                ChromeActivityEventTypes.TabObserved => "Tab observed",
                ChromeActivityEventTypes.TabChanged => "Title changed",
                ChromeActivityEventTypes.NewTab => "New tab title",
                ChromeActivityEventTypes.ForegroundResumed => "Chrome foreground",
                ChromeActivityEventTypes.FocusLost => "Chrome left foreground",
                ChromeActivityEventTypes.Snapshot => "One-off capture",
                _ => eventType
            };

        private sealed class DayOption
        {
            public DateTime Date { get; }

            public DayOption(DateTime date)
            {
                Date = date.Date;
            }

            public override string ToString() => Date.ToString("yyyy-MM-dd");
        }
    }
}
