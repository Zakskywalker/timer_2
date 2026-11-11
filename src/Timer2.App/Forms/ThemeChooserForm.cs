using System;
using System.Drawing;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.App.Forms
{
    public class ThemeChooserForm : Form
    {
        private readonly AppConfig _config;
        private readonly ConfigManager _configManager;
        private readonly Action<string> _onThemeApplied;

        private ComboBox cbThemes;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkCloseToTray;
        private CheckBox chkDisableUpdates;
        private NumericUpDown nudBackupSeconds;
        private Button btnApply;
        private Button btnCancel;

        public ThemeChooserForm(AppConfig config, ConfigManager configManager, Action<string> onThemeApplied)
        {
            _config = config;
            _configManager = configManager;
            _onThemeApplied = onThemeApplied;

            this.Text = "Settings & Theme Chooser";
            this.Size = new Size(480, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _config.Theme);
        }

        private void InitializeComponent()
        {
            int y = 20;

            var lblTitle = new Label
            {
                Text = "Appearance & Application Settings",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);
            y += 35;

            var lblTheme = new Label
            {
                Text = "Active Color Theme:",
                Location = new Point(20, y + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cbThemes = new ComboBox
            {
                Location = new Point(170, y),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (var kvp in ThemeManager.Themes)
            {
                cbThemes.Items.Add(new ThemeItem { Key = kvp.Key, Display = kvp.Value.Name });
            }

            for (int i = 0; i < cbThemes.Items.Count; i++)
            {
                if (((ThemeItem)cbThemes.Items[i]).Key.Equals(_config.Theme, StringComparison.OrdinalIgnoreCase))
                {
                    cbThemes.SelectedIndex = i;
                    break;
                }
            }
            if (cbThemes.SelectedIndex == -1 && cbThemes.Items.Count > 0)
            {
                cbThemes.SelectedIndex = 0;
            }

            cbThemes.SelectedIndexChanged += (s, e) => {
                if (cbThemes.SelectedItem is ThemeItem item)
                {
                    ThemeManager.ApplyTheme(this, item.Key);
                }
            };

            this.Controls.Add(lblTheme);
            this.Controls.Add(cbThemes);
            y += 45;

            var gbOptions = new GroupBox
            {
                Text = "Application, Tray & Update Options",
                Location = new Point(20, y),
                Size = new Size(410, 126)
            };

            chkMinimizeToTray = new CheckBox
            {
                Text = "Minimize window to notification tray",
                Location = new Point(15, 26),
                AutoSize = true,
                Checked = _config.MinimizeToTray
            };

            chkCloseToTray = new CheckBox
            {
                Text = "Close button minimizes to tray instead of quitting",
                Location = new Point(15, 54),
                AutoSize = true,
                Checked = _config.CloseToTray
            };

            chkDisableUpdates = new CheckBox
            {
                Text = "Disable automatic update check on startup (Testing mode)",
                Location = new Point(15, 82),
                AutoSize = true,
                Checked = _config.DisableAutoUpdates
            };

            gbOptions.Controls.Add(chkMinimizeToTray);
            gbOptions.Controls.Add(chkCloseToTray);
            gbOptions.Controls.Add(chkDisableUpdates);
            this.Controls.Add(gbOptions);
            y += 140;

            var lblBackup = new Label
            {
                Text = "Fault tolerance checkpoint (sec):",
                Location = new Point(20, y + 2),
                AutoSize = true
            };
            nudBackupSeconds = new NumericUpDown
            {
                Location = new Point(230, y),
                Width = 70,
                Minimum = 2,
                Maximum = 60,
                Value = Math.Max(2, Math.Min(60, _config.AutoCheckpointSeconds))
            };
            this.Controls.Add(lblBackup);
            this.Controls.Add(nudBackupSeconds);
            y += 42;

            btnApply = new Button
            {
                Text = "Save & Apply Settings",
                Location = new Point(120, y),
                Size = new Size(170, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnApply.Click += BtnApply_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(300, y),
                Size = new Size(100, 34)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnApply);
            this.Controls.Add(btnCancel);
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (cbThemes.SelectedItem is ThemeItem item)
            {
                _config.Theme = item.Key;
                _onThemeApplied?.Invoke(item.Key);
            }

            _config.MinimizeToTray = chkMinimizeToTray.Checked;
            _config.CloseToTray = chkCloseToTray.Checked;
            _config.DisableAutoUpdates = chkDisableUpdates.Checked;
            _config.AutoCheckpointSeconds = (int)nudBackupSeconds.Value;

            _configManager.SaveConfig(_config);
            this.Close();
        }

        private class ThemeItem
        {
            public string Key { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display;
        }
    }
}
