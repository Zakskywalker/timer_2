using System;
using System.Collections.Generic;
using System.Linq;

namespace Timer2.Core.Models
{
    public class DaySummary
    {
        public DateTime Date { get; set; }
        public List<TimeEntry> Entries { get; set; } = new List<TimeEntry>();

        public bool HasWarnings => Entries.Any(e => e.HasWarning);
        public int WarningCount => Entries.Count(e => e.HasWarning);

        public TimeSpan TotalTime
        {
            get
            {
                TimeSpan total = TimeSpan.Zero;
                foreach (var entry in Entries)
                {
                    total += entry.Duration;
                }
                return total;
            }
        }

        public double TotalDecimalHours => Math.Round(TotalTime.TotalHours, 2);

        public DaySummary() { }

        public DaySummary(DateTime date)
        {
            Date = date.Date;
        }
    }
}
