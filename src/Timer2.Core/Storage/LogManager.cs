using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Timer2.Core.Calculations;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public class LogManager : ILogManager
    {
        private readonly object _lock = new object();
        private readonly LogParser _parser = new LogParser();
        private LogIndexManager _indexManager;

        public string LogDirectory { get; private set; }
        public string TimeLogPath => Path.Combine(LogDirectory, "time.txt");
        public string SystemLogPath => Path.Combine(LogDirectory, "log.txt");
        public string BackupLogPath => Path.Combine(LogDirectory, "log2.txt");
        public string TempLogPath => Path.Combine(LogDirectory, "temp.txt");
        public string ChangesLogPath => Path.Combine(LogDirectory, "changes.txt");
        public string SettingsPath => Path.Combine(LogDirectory, "settings.txt");

        private List<DaySummary> _cachedDays = new List<DaySummary>();
        public bool IsInitialized { get; private set; }

        public LogManager(string directoryPath = null, bool saveCustom = true)
        {
            string targetDir = string.IsNullOrWhiteSpace(directoryPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TimerLogs")
                : directoryPath;

            InitializeDirectory(targetDir, saveCustom);
        }

        public void InitializeDirectory(string directoryPath, bool saveCustomLocation)
        {
            lock (_lock)
            {
                LogDirectory = directoryPath;
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                _indexManager = new LogIndexManager(LogDirectory);

                // Initialize files if missing
                EnsureFileExists(SystemLogPath, "*Main log not found, main log created\r\n");
                EnsureFileExists(BackupLogPath, "*Backup log created\r\n");
                EnsureFileExists(TimeLogPath, "*Time log not found, time log created.\r\n");
                EnsureFileExists(ChangesLogPath, "*Change log not found, change log created.\r\n");

                // Legacy settings.txt
                SaveLegacySettings(saveCustomLocation);

                // Initial system log entries
                AppendToLog(SystemLogPath, BackupLogPath, $"\r\n\r\n{DateTime.Now}\r\n*Directory Found\r\n*Directory Saved: {LogDirectory}\r\n*Time Recording Enabled\r\n");

                // Load existing days
                ReloadCache();
                IsInitialized = true;
            }
        }

        private void EnsureFileExists(string path, string initialHeader)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, $"{DateTime.Now}\r\n{initialHeader}", Encoding.UTF8);
            }
        }

        private void SaveLegacySettings(bool saveCustom)
        {
            try
            {
                string content = $"*Location: {LogDirectory}\r\n*SaveLoc: {saveCustom.ToString().ToLowerInvariant()}\r\n";
                File.WriteAllText(SettingsPath, content, Encoding.UTF8);
            }
            catch { }
        }

        private void ReloadCache()
        {
            if (File.Exists(TimeLogPath))
            {
                _cachedDays = _parser.ParseTimeLogFile(TimeLogPath);
            }
            else
            {
                _cachedDays = new List<DaySummary>();
            }
        }

        public DaySummary ReloadTodayRecordings()
        {
            lock (_lock)
            {
                ReloadCache();
                RefreshIndex();
                return GetTodaySummary();
            }
        }

        public void LogSystemMessage(string message, bool isError = false)
        {
            lock (_lock)
            {
                string prefix = isError ? "*ERROR: " : "*";
                string formatted = $"{prefix}{message}\r\n";
                AppendToLog(SystemLogPath, BackupLogPath, formatted);
            }
        }

        public void RecordStart(DateTime startTime)
        {
            lock (_lock)
            {
                string header = NeedDateHeader(startTime) ? $"\r\n\r\n{startTime}\r\n" : "";
                string entry = $"{header}*Start Time: {startTime}\r\n";

                AppendToLog(TimeLogPath, null, entry);
                AppendToLog(SystemLogPath, BackupLogPath, entry);

                // Write active checkpoint to temp.txt
                UpdateTempCheckpoint(startTime, TimeSpan.Zero, GetDayTotal(startTime.Date));
            }
        }

        public void UpdateTempCheckpoint(DateTime startTime, TimeSpan currentRun, TimeSpan todayTotal)
        {
            lock (_lock)
            {
                try
                {
                    string tempContent = $"*ACTIVE_SESSION*\r\n*Start Time: {startTime:yyyy-MM-dd HH:mm:ss}\r\n*Current Run: {currentRun}\r\n*Today Total: {todayTotal}\r\n*Last Checkpoint: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n";
                    File.WriteAllText(TempLogPath, tempContent, Encoding.UTF8);
                }
                catch { }
            }
        }

        public void ClearTempCheckpoint()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(TempLogPath))
                    {
                        File.Delete(TempLogPath);
                    }
                }
                catch { }
            }
        }

        public void RecordStop(DateTime startTime, DateTime stopTime, TimeSpan runDuration, TimeSpan todayTotal)
        {
            lock (_lock)
            {
                string runLine = TimeCalculator.FormatRunLine(runDuration);
                string totalLine = TimeCalculator.FormatTotalLine(todayTotal);

                string logBlock = $"*Stop Time: {stopTime}\r\n{runLine}\r\n{totalLine}\r\n\r\n";

                AppendToLog(TimeLogPath, null, logBlock);
                AppendToLog(SystemLogPath, BackupLogPath, logBlock);

                ClearTempCheckpoint();
                ReloadCache();
                RefreshIndex();
            }
        }

        public void RecordChange(TimeChangeRecord change)
        {
            lock (_lock)
            {
                string entry = $"{change}\r\n";
                try
                {
                    File.AppendAllText(ChangesLogPath, entry, Encoding.UTF8);
                    AppendToLog(SystemLogPath, BackupLogPath, $"*CHANGE LOGGED: {change.Reason}\r\n");
                }
                catch { }
            }
        }

        public void ModifyCurrentRunStart(DateTime oldStart, DateTime newStart, TimeSpan newRunDuration, TimeSpan newTodayTotal)
        {
            lock (_lock)
            {
                var change = new TimeChangeRecord
                {
                    Type = ChangeType.ModifiedCurrent,
                    EntryDate = oldStart.Date,
                    OriginalStart = oldStart,
                    NewStart = newStart,
                    TimeDifference = (newStart - oldStart),
                    Reason = $"Current run start time adjusted from {oldStart:T} to {newStart:T}"
                };

                RecordChange(change);
                UpdateTempCheckpoint(newStart, newRunDuration, newTodayTotal);
            }
        }

        public void AddManualTimePeriod(DateTime start, DateTime end, string note, out TimeSpan addedDuration, out TimeSpan newDayTotal)
        {
            lock (_lock)
            {
                addedDuration = end - start;
                DateTime date = start.Date;

                // Find or create day in cache
                var day = _cachedDays.FirstOrDefault(d => d.Date == date);
                if (day == null)
                {
                    day = new DaySummary(date);
                    _cachedDays.Add(day);
                    _cachedDays = _cachedDays.OrderBy(d => d.Date).ToList();
                }

                int runNum = day.Entries.Count + 1;
                var newEntry = new TimeEntry(start, end, runNum)
                {
                    IsModified = true,
                    Notes = note
                };
                day.Entries.Add(newEntry);
                newDayTotal = day.TotalTime;

                // Log entry to files
                string header = NeedDateHeader(start) ? $"\r\n\r\n{start}\r\n" : "";
                string manualBlock = $"{header}*Start Time: {start}\r\n*Stop Time: {end}\r\n*Manual Addition: {note}\r\n{TimeCalculator.FormatRunLine(addedDuration)}\r\n{TimeCalculator.FormatTotalLine(newDayTotal)}\r\n\r\n";

                AppendToLog(TimeLogPath, null, manualBlock);
                AppendToLog(SystemLogPath, BackupLogPath, manualBlock);

                // Audit change
                var change = new TimeChangeRecord
                {
                    Type = ChangeType.AddedManual,
                    TargetEntryId = newEntry.Id,
                    EntryDate = date,
                    NewStart = start,
                    NewStop = end,
                    TimeDifference = addedDuration,
                    Reason = $"Manually added time period: {start:g} - {end:g}. Note: {note}"
                };
                RecordChange(change);

                RefreshIndex();
            }
        }

        public void ModifyExistingTimePeriod(DateTime targetDate, int sessionIndex, DateTime newStart, DateTime newEnd, string note, out TimeSpan newDayTotal)
        {
            lock (_lock)
            {
                var day = _cachedDays.FirstOrDefault(d => d.Date == targetDate.Date);
                if (day == null || sessionIndex < 0 || sessionIndex >= day.Entries.Count)
                {
                    newDayTotal = TimeSpan.Zero;
                    return;
                }

                var entry = day.Entries[sessionIndex];
                DateTime oldStart = entry.StartTime;
                DateTime? oldEnd = entry.StopTime;
                TimeSpan oldDur = entry.Duration;

                entry.OriginalStartTime = entry.OriginalStartTime ?? oldStart;
                entry.OriginalStopTime = entry.OriginalStopTime ?? oldEnd;
                entry.StartTime = newStart;
                entry.StopTime = newEnd;
                entry.IsModified = true;
                entry.Notes = note;

                newDayTotal = day.TotalTime;
                TimeSpan diff = entry.Duration - oldDur;

                // Record change audit
                var change = new TimeChangeRecord
                {
                    Type = ChangeType.ModifiedExisting,
                    TargetEntryId = entry.Id,
                    EntryDate = targetDate.Date,
                    OriginalStart = oldStart,
                    OriginalStop = oldEnd,
                    NewStart = newStart,
                    NewStop = newEnd,
                    TimeDifference = diff,
                    Reason = $"Modified session #{entry.RunNumber} on {targetDate:d}: Old=[{oldStart:T} - {oldEnd:T}], New=[{newStart:T} - {newEnd:T}]. Note: {note}"
                };
                RecordChange(change);

                RewriteTimeLog();
                RefreshIndex();
            }
        }

        public CrashRecoveryInfo DetectCrashOrUnclosedSession()
        {
            lock (_lock)
            {
                var info = new CrashRecoveryInfo();

                // Check 1: TempLogPath
                if (File.Exists(TempLogPath))
                {
                    try
                    {
                        var lines = File.ReadAllLines(TempLogPath);
                        string startLine = lines.FirstOrDefault(l => l.StartsWith("*Start Time:"));
                        string checkpointLine = lines.FirstOrDefault(l => l.StartsWith("*Last Checkpoint:"));

                        if (!string.IsNullOrEmpty(startLine))
                        {
                            string sVal = startLine.Replace("*Start Time:", "").Trim();
                            if (DateTime.TryParse(sVal, out var sTime))
                            {
                                info.HasUnclosedSession = true;
                                info.StartTime = sTime;
                                info.Source = "TempCheckpoint";

                                if (!string.IsNullOrEmpty(checkpointLine))
                                {
                                    string cVal = checkpointLine.Replace("*Last Checkpoint:", "").Trim();
                                    if (DateTime.TryParse(cVal, out var cTime))
                                    {
                                        info.LastCheckpointTime = cTime;
                                        info.LastRecordedDuration = cTime - sTime;
                                    }
                                }
                                info.Details = $"Found active session checkpoint recorded on {sTime:g}.";
                                return info;
                            }
                        }
                    }
                    catch { }
                }

                // Check 2: Last line in time.txt without matching stop time
                if (File.Exists(TimeLogPath))
                {
                    try
                    {
                        var allLines = File.ReadAllLines(TimeLogPath).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
                        int lastStartIndex = allLines.FindLastIndex(l => l.StartsWith("*Start Time:"));
                        int lastStopIndex = allLines.FindLastIndex(l => l.StartsWith("*Stop Time:") || l.StartsWith("*Stope Time:"));

                        if (lastStartIndex >= 0 && lastStartIndex > lastStopIndex)
                        {
                            string sLine = allLines[lastStartIndex];
                            string sVal = sLine.Replace("*Start Time:", "").Trim();
                            if (DateTime.TryParse(sVal, out var sTime))
                            {
                                info.HasUnclosedSession = true;
                                info.StartTime = sTime;
                                info.Source = "UnclosedLogEntry";
                                info.Details = $"Found unclosed *Start Time in time.txt recorded on {sTime:g}.";
                                return info;
                            }
                        }
                    }
                    catch { }
                }

                return info;
            }
        }

        public void AutoHealCloseLastTimestamp(DateTime startTime, DateTime stopTime, out TimeSpan closedDuration, out TimeSpan newDayTotal)
        {
            lock (_lock)
            {
                closedDuration = stopTime - startTime;
                if (closedDuration < TimeSpan.Zero) closedDuration = TimeSpan.Zero;

                DateTime date = startTime.Date;
                var day = _cachedDays.FirstOrDefault(d => d.Date == date);
                if (day == null)
                {
                    day = new DaySummary(date);
                    _cachedDays.Add(day);
                }

                newDayTotal = day.TotalTime + closedDuration;

                // Append Stop time and updated totals
                string runLine = TimeCalculator.FormatRunLine(closedDuration);
                string totalLine = TimeCalculator.FormatTotalLine(newDayTotal);
                string healBlock = $"*Stop Time: {stopTime}\r\n*AutoHeal Recovery: Closed previously interrupted session\r\n{runLine}\r\n{totalLine}\r\n\r\n";

                AppendToLog(TimeLogPath, null, healBlock);
                AppendToLog(SystemLogPath, BackupLogPath, healBlock);

                // Audit log
                var change = new TimeChangeRecord
                {
                    Type = ChangeType.ModifiedExisting,
                    EntryDate = date,
                    NewStart = startTime,
                    NewStop = stopTime,
                    TimeDifference = closedDuration,
                    Reason = $"Auto-healed and closed unclosed session from {startTime:g} to {stopTime:g} ({TimeCalculator.FormatDecimalHours(closedDuration)})"
                };
                RecordChange(change);

                ClearTempCheckpoint();
                ReloadCache();
                RefreshIndex();
            }
        }

        public void AutoHealDiscardSession()
        {
            lock (_lock)
            {
                ClearTempCheckpoint();
                LogSystemMessage("Discarded unclosed interrupted timer session during auto-heal recovery.");
                ReloadCache();
                RefreshIndex();
            }
        }

        private void RewriteTimeLog()
        {
            var sb = new StringBuilder();
            foreach (var day in _cachedDays.OrderBy(d => d.Date))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine($"{day.Date:M/d/yyyy} 12:00:00 AM");

                TimeSpan runningTotal = TimeSpan.Zero;
                foreach (var entry in day.Entries)
                {
                    sb.AppendLine($"*Start Time: {entry.StartTime}");
                    if (entry.StopTime.HasValue)
                    {
                        runningTotal += entry.Duration;
                        sb.AppendLine($"*Stop Time: {entry.StopTime.Value}");
                        if (entry.IsModified)
                        {
                            sb.AppendLine($"*Modified Entry: {entry.Notes}");
                        }
                        sb.AppendLine(TimeCalculator.FormatRunLine(entry.Duration));
                        sb.AppendLine(TimeCalculator.FormatTotalLine(runningTotal));
                        sb.AppendLine();
                    }
                }
            }

            File.WriteAllText(TimeLogPath, sb.ToString(), Encoding.UTF8);
        }

        private bool NeedDateHeader(DateTime timestamp)
        {
            if (!File.Exists(TimeLogPath)) return true;
            try
            {
                var lastLines = File.ReadLines(TimeLogPath).Reverse().Take(20).ToList();
                string datePattern = timestamp.ToString("M/d/yyyy");
                return !lastLines.Any(l => l.Contains(datePattern));
            }
            catch
            {
                return true;
            }
        }

        private TimeSpan GetDayTotal(DateTime date)
        {
            var day = _cachedDays.FirstOrDefault(d => d.Date == date.Date);
            return day != null ? day.TotalTime : TimeSpan.Zero;
        }

        public DaySummary GetTodaySummary()
        {
            lock (_lock)
            {
                DateTime today = DateTime.Today;
                var day = _cachedDays.FirstOrDefault(d => d.Date == today);
                if (day == null)
                {
                    day = new DaySummary(today);
                    _cachedDays.Add(day);
                }
                return day;
            }
        }

        public List<DaySummary> GetAllDays()
        {
            lock (_lock)
            {
                return new List<DaySummary>(_cachedDays);
            }
        }

        public LogIndex RefreshIndex()
        {
            lock (_lock)
            {
                var index = _indexManager.BuildIndex(_cachedDays);
                _indexManager.SaveIndex(index);
                return index;
            }
        }

        private void AppendToLog(string primaryPath, string backupPath, string content)
        {
            try
            {
                File.AppendAllText(primaryPath, content, Encoding.UTF8);
            }
            catch { }

            if (!string.IsNullOrEmpty(backupPath))
            {
                try
                {
                    File.AppendAllText(backupPath, content, Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}
