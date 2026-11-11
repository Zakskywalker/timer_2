using System;
using System.Collections.Generic;

namespace Timer2.Core.Models
{
    public class DateIndexItem
    {
        public string DateString { get; set; } = string.Empty; // MM/dd/yyyy
        public DateTime Date { get; set; }
        public int SessionCount { get; set; }
        public double TotalHours { get; set; }
        public string FormattedTotal { get; set; } = string.Empty;
        public List<TimeEntry> Sessions { get; set; } = new List<TimeEntry>();
    }

    public class LogIndex
    {
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public int TotalRecordedDays => Days.Count;
        public double LifetimeTotalHours { get; set; }
        public List<DateIndexItem> Days { get; set; } = new List<DateIndexItem>();
    }
}
