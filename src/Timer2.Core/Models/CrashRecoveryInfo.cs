using System;

namespace Timer2.Core.Models
{
    public class CrashRecoveryInfo
    {
        public bool HasUnclosedSession { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? LastCheckpointTime { get; set; }
        public TimeSpan LastRecordedDuration { get; set; }
        public string Source { get; set; } = "None"; // "TempCheckpoint" or "UnclosedLogEntry"
        public string Details { get; set; } = string.Empty;
    }
}

