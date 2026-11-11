using System;
using System.Collections.Generic;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public interface ILogManager
    {
        string LogDirectory { get; }
        string TimeLogPath { get; }
        string SystemLogPath { get; }
        string BackupLogPath { get; }
        string TempLogPath { get; }
        string ChangesLogPath { get; }
        string SettingsPath { get; }

        void InitializeDirectory(string directoryPath, bool saveCustomLocation);
        void LogSystemMessage(string message, bool isError = false);
        void RecordStart(DateTime startTime);
        void UpdateTempCheckpoint(DateTime startTime, TimeSpan currentRun, TimeSpan todayTotal);
        void RecordStop(DateTime startTime, DateTime stopTime, TimeSpan runDuration, TimeSpan todayTotal);
        void RecordChange(TimeChangeRecord change);
        void ModifyCurrentRunStart(DateTime oldStart, DateTime newStart, TimeSpan newRunDuration, TimeSpan newTodayTotal);
        void AddManualTimePeriod(DateTime start, DateTime end, string note, out TimeSpan addedDuration, out TimeSpan newDayTotal);
        void ModifyExistingTimePeriod(DateTime targetDate, int sessionIndex, DateTime newStart, DateTime newEnd, string note, out TimeSpan newDayTotal);
        DaySummary GetTodaySummary();
        List<DaySummary> GetAllDays();
        LogIndex RefreshIndex();
        void ClearTempCheckpoint();

        // Fault Tolerance & Auto-healing
        CrashRecoveryInfo DetectCrashOrUnclosedSession();
        void AutoHealCloseLastTimestamp(DateTime startTime, DateTime stopTime, out TimeSpan closedDuration, out TimeSpan newDayTotal);
        void AutoHealDiscardSession();
        DaySummary ReloadTodayRecordings();
    }
}
