using System;

namespace Timer2.Core.Models
{
    public enum ChangeType
    {
        Created,
        ModifiedCurrent,
        AddedManual,
        ModifiedExisting
    }

    public class TimeChangeRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ChangeType Type { get; set; }
        public string TargetEntryId { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public DateTime? OriginalStart { get; set; }
        public DateTime? OriginalStop { get; set; }
        public DateTime NewStart { get; set; }
        public DateTime? NewStop { get; set; }
        public TimeSpan TimeDifference { get; set; }
        public string Reason { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] TYPE={Type} DATE={EntryDate:MM/dd/yyyy} START={NewStart:T} END={(NewStop.HasValue ? NewStop.Value.ToString("T") : "RUNNING")} NOTE={Reason}";
        }
    }
}
