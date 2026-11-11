using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Timer2.Core.Models;

namespace Timer2.Core.Database
{
    public class MySqlDataConnector : IDataConnector
    {
        private readonly MySqlConfig _config;

        public bool IsConfigured => _config != null && !string.IsNullOrWhiteSpace(_config.Server) && !string.IsNullOrWhiteSpace(_config.Database);

        public MySqlDataConnector(MySqlConfig config)
        {
            _config = config ?? new MySqlConfig();
        }

        private MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_config.BuildConnectionString());
        }

        private string GetTimerTableName(string overrideTable)
        {
            string t = !string.IsNullOrWhiteSpace(overrideTable) ? overrideTable : _config.TimerLogsTable;
            return string.IsNullOrWhiteSpace(t) ? "timer_sessions" : t.Trim();
        }

        private string GetSystemTableName(string overrideTable)
        {
            string t = !string.IsNullOrWhiteSpace(overrideTable) ? overrideTable : _config.SystemLogsTable;
            return string.IsNullOrWhiteSpace(t) ? "timer_system_logs" : t.Trim();
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                return (true, $"Connected to MySQL database '{_config.Database}' at {_config.Server}:{_config.Port}.");
            }
            catch (Exception ex)
            {
                return (false, $"MySQL connection failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> InitializeSchemaAsync()
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string timerTable = GetTimerTableName(null);
                string systemTable = GetSystemTableName(null);

                string ddl = $@"
CREATE TABLE IF NOT EXISTS `{timerTable}` (
    id VARCHAR(64) PRIMARY KEY,
    session_date DATE NOT NULL,
    run_number INT NOT NULL,
    start_time DATETIME NOT NULL,
    stop_time DATETIME NULL,
    duration_seconds INT NOT NULL DEFAULT 0,
    decimal_hours DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    is_modified TINYINT(1) NOT NULL DEFAULT 0,
    has_warning TINYINT(1) NOT NULL DEFAULT 0,
    warning_type VARCHAR(64) NULL,
    warning_details TEXT NULL,
    notes TEXT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_date (session_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{systemTable}` (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    log_level VARCHAR(16) NOT NULL DEFAULT 'INFO',
    message TEXT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";

                using var cmd = new MySqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync();

                return (true, $"Schema initialized: tables `{timerTable}` and `{systemTable}` ready.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to initialize schema: {ex.Message}");
            }
        }

        // Dedicated Export for Timer Logs
        public async Task<(int ExportedCount, string Message)> ExportTimerLogsToDbAsync(List<TimeEntry> entries, string tableName = null)
        {
            if (entries == null || entries.Count == 0)
            {
                return (0, "No timer entries to export.");
            }

            string table = GetTimerTableName(tableName);
            int count = 0;
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string sql = $@"
INSERT INTO `{table}`
    (id, session_date, run_number, start_time, stop_time, duration_seconds, decimal_hours, is_modified, has_warning, warning_type, warning_details, notes)
VALUES
    (@id, @date, @runNumber, @startTime, @stopTime, @durationSeconds, @decimalHours, @isModified, @hasWarning, @warningType, @warningDetails, @notes)
ON DUPLICATE KEY UPDATE
    start_time = VALUES(start_time),
    stop_time = VALUES(stop_time),
    duration_seconds = VALUES(duration_seconds),
    decimal_hours = VALUES(decimal_hours),
    is_modified = VALUES(is_modified),
    has_warning = VALUES(has_warning),
    warning_type = VALUES(warning_type),
    warning_details = VALUES(warning_details),
    notes = VALUES(notes);";

                foreach (var entry in entries)
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", entry.Id);
                    cmd.Parameters.AddWithValue("@date", entry.Date);
                    cmd.Parameters.AddWithValue("@runNumber", entry.RunNumber);
                    cmd.Parameters.AddWithValue("@startTime", entry.StartTime);
                    cmd.Parameters.AddWithValue("@stopTime", (object)entry.StopTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@durationSeconds", (int)entry.Duration.TotalSeconds);
                    cmd.Parameters.AddWithValue("@decimalHours", entry.DecimalHours);
                    cmd.Parameters.AddWithValue("@isModified", entry.IsModified ? 1 : 0);
                    cmd.Parameters.AddWithValue("@hasWarning", entry.HasWarning ? 1 : 0);
                    cmd.Parameters.AddWithValue("@warningType", (object)entry.WarningType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@warningDetails", (object)entry.WarningDetails ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object)entry.Notes ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    count++;
                }

                return (count, $"Exported {count} timer session(s) into table `{table}`.");
            }
            catch (Exception ex)
            {
                return (count, $"Error exporting timer logs: {ex.Message}");
            }
        }

        // Dedicated Export for System Logs
        public async Task<(int ExportedCount, string Message)> ExportSystemLogsToDbAsync(List<string> logLines, string tableName = null)
        {
            if (logLines == null || logLines.Count == 0)
            {
                return (0, "No system log lines to export.");
            }

            string table = GetSystemTableName(tableName);
            int count = 0;
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string sql = $"INSERT INTO `{table}` (log_level, message) VALUES (@level, @message);";

                foreach (var line in logLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string level = line.Contains("ERROR") ? "ERROR" : (line.Contains("WARN") ? "WARN" : "INFO");

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@level", level);
                    cmd.Parameters.AddWithValue("@message", line.Trim());

                    await cmd.ExecuteNonQueryAsync();
                    count++;
                }

                return (count, $"Exported {count} system log line(s) into table `{table}`.");
            }
            catch (Exception ex)
            {
                return (count, $"Error exporting system logs: {ex.Message}");
            }
        }

        // Retrieve Timer Logs from DB
        public async Task<List<TimeEntry>> RetrieveTimerLogsFromDbAsync(string tableName = null)
        {
            var list = new List<TimeEntry>();
            string table = GetTimerTableName(tableName);
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string sql = $"SELECT id, session_date, run_number, start_time, stop_time, duration_seconds, is_modified, has_warning, warning_type, warning_details, notes FROM `{table}` ORDER BY session_date DESC, start_time DESC;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var entry = new TimeEntry
                    {
                        Id = reader.GetString("id"),
                        Date = reader.GetDateTime("session_date"),
                        RunNumber = reader.GetInt32("run_number"),
                        StartTime = reader.GetDateTime("start_time"),
                        StopTime = reader.IsDBNull(reader.GetOrdinal("stop_time")) ? (DateTime?)null : reader.GetDateTime("stop_time"),
                        IsModified = reader.GetBoolean("is_modified"),
                        HasWarning = reader.GetBoolean("has_warning"),
                        WarningType = reader.IsDBNull(reader.GetOrdinal("warning_type")) ? "" : reader.GetString("warning_type"),
                        WarningDetails = reader.IsDBNull(reader.GetOrdinal("warning_details")) ? "" : reader.GetString("warning_details"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString("notes")
                    };
                    list.Add(entry);
                }
            }
            catch { }
            return list;
        }

        // Retrieve System Logs from DB
        public async Task<List<DbSystemLogItem>> RetrieveSystemLogsFromDbAsync(string tableName = null)
        {
            var list = new List<DbSystemLogItem>();
            string table = GetSystemTableName(tableName);
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string sql = $"SELECT id, log_time, log_level, message FROM `{table}` ORDER BY id DESC LIMIT 500;";
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new DbSystemLogItem
                    {
                        Id = reader.GetInt64("id"),
                        LogTime = reader.GetDateTime("log_time"),
                        Level = reader.GetString("log_level"),
                        Message = reader.GetString("message")
                    });
                }
            }
            catch { }
            return list;
        }

        // Compare Local Logs vs DB Table
        public async Task<(DbComparisonResult Result, string Message)> CompareTimerLogsAsync(List<DaySummary> localDays, string tableName = null)
        {
            string table = GetTimerTableName(tableName);
            var result = new DbComparisonResult
            {
                TimerTableName = table,
                SystemTableName = GetSystemTableName(null)
            };

            try
            {
                var dbEntries = await RetrieveTimerLogsFromDbAsync(table);
                var localEntries = localDays.SelectMany(d => d.Entries).ToList();

                result.LocalTotal = localEntries.Count;
                result.DbTotal = dbEntries.Count;

                var dbMap = dbEntries.ToDictionary(e => e.Id, e => e);

                // Check local items against DB
                foreach (var loc in localEntries)
                {
                    var item = new DbTimerComparisonItem
                    {
                        SessionId = loc.Id,
                        Date = loc.Date,
                        RunNumber = loc.RunNumber,
                        LocalStart = loc.StartTime,
                        LocalStop = loc.StopTime,
                        LocalDuration = loc.Duration
                    };

                    if (!dbMap.TryGetValue(loc.Id, out var dbItem))
                    {
                        // Check if matched by date and start time within 2 seconds
                        dbItem = dbEntries.FirstOrDefault(d => d.Date == loc.Date && Math.Abs((d.StartTime - loc.StartTime).TotalSeconds) < 2);
                    }

                    if (dbItem == null)
                    {
                        item.Status = DbSyncStatus.ToUpload;
                        item.StatusDescription = "New local session (not in database)";
                        result.ToUploadCount++;
                    }
                    else
                    {
                        item.DbStart = dbItem.StartTime;
                        item.DbStop = dbItem.StopTime;
                        item.DbDuration = dbItem.Duration;

                        // Check equality
                        bool stopEqual = (loc.StopTime.HasValue == dbItem.StopTime.HasValue) &&
                                         (!loc.StopTime.HasValue || Math.Abs((loc.StopTime.Value - dbItem.StopTime.Value).TotalSeconds) < 2);

                        if (stopEqual && Math.Abs((loc.StartTime - dbItem.StartTime).TotalSeconds) < 2)
                        {
                            item.Status = DbSyncStatus.InSync;
                            item.StatusDescription = "Synchronized";
                            result.InSyncCount++;
                        }
                        else
                        {
                            item.Status = DbSyncStatus.Modified;
                            item.StatusDescription = "Modified timestamps differ from DB";
                            result.ModifiedCount++;
                        }
                    }

                    result.Items.Add(item);
                }

                // Check for items that exist in DB only
                var localIds = new HashSet<string>(localEntries.Select(e => e.Id));
                foreach (var dbOnly in dbEntries.Where(d => !localIds.Contains(d.Id)))
                {
                    bool alreadyMatched = result.Items.Any(i => i.DbStart.HasValue && Math.Abs((i.DbStart.Value - dbOnly.StartTime).TotalSeconds) < 2);
                    if (!alreadyMatched)
                    {
                        result.Items.Add(new DbTimerComparisonItem
                        {
                            SessionId = dbOnly.Id,
                            Date = dbOnly.Date,
                            RunNumber = dbOnly.RunNumber,
                            DbStart = dbOnly.StartTime,
                            DbStop = dbOnly.StopTime,
                            DbDuration = dbOnly.Duration,
                            Status = DbSyncStatus.InDbOnly,
                            StatusDescription = "Exists in database only"
                        });
                        result.InDbOnlyCount++;
                    }
                }

                return (result, $"Comparison complete against table `{table}`: {result.ToUploadCount} to upload, {result.ModifiedCount} modified, {result.InSyncCount} in sync, {result.InDbOnlyCount} in DB only.");
            }
            catch (Exception ex)
            {
                return (result, $"Comparison failed: {ex.Message}");
            }
        }

        public async Task<(int SubmittedCount, string Message)> SubmitTimerLogsToDbAsync(List<TimeEntry> entriesToSubmit, string tableName = null)
        {
            return await ExportTimerLogsToDbAsync(entriesToSubmit, tableName);
        }

        public async Task<(int SyncedCount, string Message)> SyncSessionsAsync(List<DaySummary> days)
        {
            var entries = days.SelectMany(d => d.Entries).ToList();
            return await ExportTimerLogsToDbAsync(entries);
        }

        public async Task<(int SyncedCount, string Message)> SyncChangesAsync(List<TimeChangeRecord> changes)
        {
            return await Task.FromResult((0, "Not implemented for basic sync"));
        }

        public async Task<(bool Success, string Message)> LogEventAsync(string level, string message)
        {
            var (cnt, msg) = await ExportSystemLogsToDbAsync(new List<string> { message });
            return (cnt > 0, msg);
        }
    }
}
