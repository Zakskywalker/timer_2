using System;
using System.Collections.Generic;

namespace Timer2.Core.Models
{
    public enum DbSyncStatus
    {
        InSync,
        ToUpload,      // Exists locally, missing in DB
        Modified,      // Exists in both, but timestamps/durations differ
        InDbOnly       // Exists in DB, missing locally
    }

    public class DbTimerComparisonItem
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int RunNumber { get; set; }
        public DateTime LocalStart { get; set; }
        public DateTime? LocalStop { get; set; }
        public DateTime? DbStart { get; set; }
        public DateTime? DbStop { get; set; }
        public TimeSpan LocalDuration { get; set; }
        public TimeSpan DbDuration { get; set; }
        public DbSyncStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
    }

    public class DbSystemLogItem
    {
        public long Id { get; set; }
        public DateTime LogTime { get; set; }
        public string Level { get; set; } = "INFO";
        public string Message { get; set; } = string.Empty;
    }

    public class DbComparisonResult
    {
        public DateTime ComparedAt { get; set; } = DateTime.Now;
        public string TimerTableName { get; set; } = string.Empty;
        public string SystemTableName { get; set; } = string.Empty;

        public int LocalTotal { get; set; }
        public int DbTotal { get; set; }
        public int ToUploadCount { get; set; }
        public int InSyncCount { get; set; }
        public int ModifiedCount { get; set; }
        public int InDbOnlyCount { get; set; }

        public List<DbTimerComparisonItem> Items { get; set; } = new List<DbTimerComparisonItem>();
    }
}

