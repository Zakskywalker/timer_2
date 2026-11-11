using System;

namespace Timer2.Core.Models
{
    public class TimeEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Date { get; set; }
        public int RunNumber { get; set; } = 1;
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        
        public bool HasWarning { get; set; }
        public string WarningType { get; set; } = string.Empty; // "MissingStop", "NegativeDuration", "Overlapping"
        public string WarningDetails { get; set; } = string.Empty;

        public TimeSpan Duration
        {
            get
            {
                if (!StopTime.HasValue)
                {
                    return TimeSpan.Zero;
                }
                var dur = StopTime.Value - StartTime;
                return dur < TimeSpan.Zero ? TimeSpan.Zero : dur;
            }
        }

        public double DecimalHours => Math.Round(Duration.TotalHours, 2);
        public bool IsModified { get; set; }
        public DateTime? OriginalStartTime { get; set; }
        public DateTime? OriginalStopTime { get; set; }
        public string Notes { get; set; } = string.Empty;

        public TimeEntry() { }

        public TimeEntry(DateTime start, DateTime? stop = null, int runNumber = 1)
        {
            Date = start.Date;
            StartTime = start;
            StopTime = stop;
            RunNumber = runNumber;
            if (!stop.HasValue)
            {
                HasWarning = true;
                WarningType = "MissingStop";
                WarningDetails = "No stop time recorded for this session.";
            }
        }
    }
}
