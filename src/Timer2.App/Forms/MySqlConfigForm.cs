using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer2.App.UI;
using Timer2.Core.Database;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.App.Forms
{
    public class MySqlConfigForm : Form
    {
        private readonly AppConfig _config;
        private readonly ConfigManager _configManager;
        private readonly ILogManager _logManager;
        private readonly string _themeName;

        private CheckBox chkEnableMySql;
        private TextBox txtServer;
        private NumericUpDown nudPort;
        private TextBox txtDatabase;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private CheckBox chkAutoSync;

        private Button btnTestConnection;
        private Button btnInitSchema;
        private Button btnSyncNow;
        private Button btnSave;
        private Label lblStatus;

        public MySqlConfigForm(AppConfig config, ConfigManager configManager, ILogManager logManager, string themeName)
        {
            _config = config;
            _configManager = configManager;
            _logManager = logManager;
            _themeName = themeName;

            this.Text = "MySQL Connector & Synchronization Setup";
            this.Size = new Size(500, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _themeName);
        }

        private void InitializeComponent()
        {
            int y = 20;

            chkEnableMySql = new CheckBox
            {
                Text = "Enable MySQL Database Synchronization",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(25, y),
                AutoSize = true,
                Checked = _config.MySql.Enabled
            };
            chkEnableMySql.CheckedChanged += (s, e) => ToggleInputs();
            this.Controls.Add(chkEnableMySql);
            y += 40;

            // Server
            var lblServer = new Label { Text = "Host / Server:", Location = new Point(25, y + 3), AutoSize = true };
            txtServer = new TextBox { Text = _config.MySql.Server, Location = new Point(140, y), Width = 180 };
            
            // Port
            var lblPort = new Label { Text = "Port:", Location = new Point(335, y + 3), AutoSize = true };
            nudPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = _config.MySql.Port, Location = new Point(375, y), Width = 75 };
            this.Controls.AddRange(new Control[] { lblServer, txtServer, lblPort, nudPort });
            y += 35;

            // Database
            var lblDb = new Label { Text = "Database Name:", Location = new Point(25, y + 3), AutoSize = true };
            txtDatabase = new TextBox { Text = _config.MySql.Database, Location = new Point(140, y), Width = 310 };
            this.Controls.AddRange(new Control[] { lblDb, txtDatabase });
            y += 35;

            // Username
            var lblUser = new Label { Text = "Username:", Location = new Point(25, y + 3), AutoSize = true };
            txtUsername = new TextBox { Text = _config.MySql.Username, Location = new Point(140, y), Width = 310 };
            this.Controls.AddRange(new Control[] { lblUser, txtUsername });
            y += 35;

            // Password
            var lblPass = new Label { Text = "Password:", Location = new Point(25, y + 3), AutoSize = true };
            txtPassword = new TextBox { Text = _config.MySql.Password, Location = new Point(140, y), Width = 310, UseSystemPasswordChar = true };
            this.Controls.AddRange(new Control[] { lblPass, txtPassword });
            y += 35;

            chkAutoSync = new CheckBox
            {
                Text = "Automatically sync sessions to MySQL on timer stop / modify",
                Location = new Point(25, y),
                AutoSize = true,
                Checked = _config.MySql.AutoSyncOnStop
            };
            this.Controls.Add(chkAutoSync);
            y += 40;

            btnTestConnection = new Button { Text = "Test Connection", Location = new Point(25, y), Width = 135, Height = 32 };
            btnTestConnection.Click += async (s, e) => await TestConnectionAsync();

            btnInitSchema = new Button { Text = "Initialize Schema", Location = new Point(170, y), Width = 135, Height = 32 };
            btnInitSchema.Click += async (s, e) => await InitSchemaAsync();

            btnSyncNow = new Button { Text = "Sync All Now", Location = new Point(315, y), Width = 135, Height = 32 };
            btnSyncNow.Click += async (s, e) => await SyncNowAsync();

            this.Controls.AddRange(new Control[] { btnTestConnection, btnInitSchema, btnSyncNow });
            y += 45;

            lblStatus = new Label
            {
                Text = "Status: Ready",
                Location = new Point(25, y),
                Size = new Size(425, 40),
                Font = new Font("Segoe UI", 9f)
            };
            this.Controls.Add(lblStatus);
            y += 45;

            btnSave = new Button
            {
                Text = "Save MySQL Configuration",
                Location = new Point(120, y),
                Size = new Size(240, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => SaveSettings();
            this.Controls.Add(btnSave);

            ToggleInputs();
        }

        private void ToggleInputs()
        {
            bool en = chkEnableMySql.Checked;
            txtServer.Enabled = en;
            nudPort.Enabled = en;
            txtDatabase.Enabled = en;
            txtUsername.Enabled = en;
            txtPassword.Enabled = en;
            chkAutoSync.Enabled = en;
            btnTestConnection.Enabled = en;
            btnInitSchema.Enabled = en;
            btnSyncNow.Enabled = en;
        }

        private MySqlConfig GetFormConfig()
        {
            return new MySqlConfig
            {
                Enabled = chkEnableMySql.Checked,
                Server = txtServer.Text.Trim(),
                Port = (int)nudPort.Value,
                Database = txtDatabase.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                AutoSyncOnStop = chkAutoSync.Checked
            };
        }

        private async Task TestConnectionAsync()
        {
            lblStatus.Text = "Testing connection...";
            lblStatus.ForeColor = Color.Gold;
            btnTestConnection.Enabled = false;

            var connector = new MySqlDataConnector(GetFormConfig());
            var (success, msg) = await connector.TestConnectionAsync();

            lblStatus.Text = msg;
            lblStatus.ForeColor = success ? Color.LightGreen : Color.Tomato;
            btnTestConnection.Enabled = true;
        }

        private async Task InitSchemaAsync()
        {
            lblStatus.Text = "Initializing tables (timer_sessions, timer_changes, timer_logs)...";
            btnInitSchema.Enabled = false;

            var connector = new MySqlDataConnector(GetFormConfig());
            var (success, msg) = await connector.InitializeSchemaAsync();

            lblStatus.Text = msg;
            lblStatus.ForeColor = success ? Color.LightGreen : Color.Tomato;
            btnInitSchema.Enabled = true;
        }

        private async Task SyncNowAsync()
        {
            lblStatus.Text = "Syncing all local sessions to MySQL...";
            btnSyncNow.Enabled = false;

            var connector = new MySqlDataConnector(GetFormConfig());
            var days = _logManager.GetAllDays();
            var (count, msg) = await connector.SyncSessionsAsync(days);

            lblStatus.Text = msg;
            lblStatus.ForeColor = count > 0 ? Color.LightGreen : Color.Tomato;
            btnSyncNow.Enabled = true;
        }

        private void SaveSettings()
        {
            _config.MySql = GetFormConfig();
            _configManager.SaveConfig(_config);
            MessageBox.Show("MySQL configuration saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
