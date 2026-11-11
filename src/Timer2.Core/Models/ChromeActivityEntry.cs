using System;

namespace Timer2.Core.Models
{
    public static class ChromeActivityEventTypes
    {
        public const string RecordingStarted = "recording-started";
        public const string RecordingStopped = "recording-stopped";
        public const string TabObserved = "tab-observed";
        public const string TabChanged = "tab-changed";
        public const string NewTab = "new-tab";
        public const string ForegroundResumed = "foreground-resumed";
        public const string FocusLost = "focus-lost";
        public const string Snapshot = "snapshot";
    }

    public class ChromeActivityEntry
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = ChromeActivityEventTypes.TabObserved;
        public string WindowTitle { get; set; } = string.Empty;
    }
}
