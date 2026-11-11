using System;
using System.Drawing;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Calculations;
using Timer2.Core.Models;

namespace Timer2.App.Forms
{
    public enum AutoHealAction
    {
        ContinueRecording,
        CloseLastTimestamp,
        Discard
    }

    public class AutoHealRecoveryDialog : Form
    {
        public AutoHealAction SelectedAction { get; private set; } = AutoHealAction.ContinueRecording;
        public DateTime ChosenStopTime { get; private set; }

        public AutoHealRecoveryDialog(CrashRecoveryInfo info, string themeName)
        {
            this.Text = "Fault Tolerance — Auto-Healing & Crash Recovery";
            this.Size = new Size(560, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeComponent(info);
            ThemeManager.ApplyTheme(this, themeName);
        }

        private void InitializeComponent(CrashRecoveryInfo info)
        {
            int y = 20;

            var lblHeader = new Label
            {
                Text = "⚡ Interrupted Recording Detected",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);
            y += 35;

            var lblDesc = new Label
            {
                Text = "The application detected a timer session that was not cleanly stopped (due to an unexpected system shutdown, reboot, or application crash).",
                Location = new Point(20, y),
                Size = new Size(500, 36),
                Font = new Font("Segoe UI", 9.5f)
            };
            this.Controls.Add(lblDesc);
            y += 45;

            var gbDetails = new GroupBox
            {
                Text = "Verified Session Timestamps",
                Location = new Point(20, y),
                Size = new Size(500, 115)
            };

            var lblStart = new Label
            {
                Text = $"• Start Time: {info.StartTime:MM/dd/yyyy hh:mm:ss tt}",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            string lastCheckStr = info.LastCheckpointTime.HasValue 
                ? $"{info.LastCheckpointTime.Value:MM/dd/yyyy hh:mm:ss tt} ({TimeCalculator.FormatHM(info.LastRecordedDuration, true)})"
                : "Not available";

            var lblCheckpoint = new Label
            {
                Text = $"• Last Checkpoint: {lastCheckStr}",
                Location = new Point(15, 52),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblStatus = new Label
            {
                Text = $"• Source: {info.Source} — Integrity verified",
                Location = new Point(15, 78),
                AutoSize = true,
                ForeColor = Color.MediumSeaGreen,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };

            gbDetails.Controls.AddRange(new Control[] { lblStart, lblCheckpoint, lblStatus });
            this.Controls.Add(gbDetails);
            y += 130;

            var lblQuestion = new Label
            {
                Text = "How would you like to handle this session?",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblQuestion);
            y += 35;

            // Button 1: Continue Recording
            var btnContinue = new Button
            {
                Text = "▶ Continue Recording\r\n(Resume live timer & today's console)",
                Location = new Point(20, y),
                Size = new Size(240, 52),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnContinue.Click += (s, e) => {
                SelectedAction = AutoHealAction.ContinueRecording;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Button 2: Close Last Timestamp
            DateTime defaultStop = info.LastCheckpointTime ?? DateTime.Now;
            ChosenStopTime = defaultStop;

            var btnCloseTimestamp = new Button
            {
                Text = "⏹ Close Last Timestamp\r\n(Finalize session to checkpoint time)",
                Location = new Point(280, y),
                Size = new Size(240, 52),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnCloseTimestamp.Click += (s, e) => {
                SelectedAction = AutoHealAction.CloseLastTimestamp;
                ChosenStopTime = defaultStop;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(btnContinue);
            this.Controls.Add(btnCloseTimestamp);
            y += 65;

            // Discard link
            var btnDiscard = new Button
            {
                Text = "Discard This Session",
                Location = new Point(180, y),
                Size = new Size(180, 28),
                Font = new Font("Segoe UI", 8.5f)
            };
            btnDiscard.Click += (s, e) => {
                var c = MessageBox.Show("Are you sure you want to discard this unclosed session?", "Confirm Discard", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (c == DialogResult.Yes)
                {
                    SelectedAction = AutoHealAction.Discard;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            this.Controls.Add(btnDiscard);
        }
    }
}

