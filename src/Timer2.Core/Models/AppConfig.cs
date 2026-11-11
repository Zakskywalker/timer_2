using System;
using System.IO;

namespace Timer2.Core.Models
{
    public class MySqlConfig
    {
        public bool Enabled { get; set; } = false;
        public string Server { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "timerdb";
        public string Username { get; set; } = "root";
        public string Password { get; set; } = string.Empty;

        // Dedicated table names for timer logs and system logs
        public string TimerLogsTable { get; set; } = "timer_sessions";
        public string SystemLogsTable { get; set; } = "timer_system_logs";

        public bool AutoSyncOnStop { get; set; } = true;
        public int ConnectionTimeoutSeconds { get; set; } = 5;

        public string BuildConnectionString()
        {
            return $"Server={Server};Port={Port};Database={Database};User ID={Username};Password={Password};Connection Timeout={ConnectionTimeoutSeconds};";
        }
    }

    public class AppConfig
    {
        public string OutputDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "TimerLogs");
        
        public bool SaveCustomLocation { get; set; } = true;
        public string Theme { get; set; } = "CyberGreen"; // Default theme
        public string DefaultCopyFormat { get; set; } = "HP"; // HP (hours.percent) or HM (hours:minutes)
        public int AutoCheckpointSeconds { get; set; } = 10;
        public bool MinimizeToTray { get; set; } = true;
        public bool CloseToTray { get; set; } = false;

        /// <summary>
        /// When true, disables automatic update checks and downloads on application startup (ideal for testing/development).
        /// Can also be overridden by passing --no-update CLI argument or setting TIMER2_DISABLE_UPDATE=1.
        /// </summary>
        public bool DisableAutoUpdates { get; set; } = false;

        public MySqlConfig MySql { get; set; } = new MySqlConfig();

        public static string DefaultConfigFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "appsettings.json");
    }
}
