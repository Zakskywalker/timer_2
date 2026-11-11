using System;
using System.IO;
using Timer2.Core.Export;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.Tests
{
    public class ChromeActivityTests
    {
        [Fact]
        public void ChromeActivityLog_WritesAndReadsSeparateDailyTextLogs()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"Timer2ChromeLog_{Guid.NewGuid():N}");
            try
            {
                var timerLog = new LogManager(directory);
                var chromeLog = new ChromeActivityLog(timerLog);
                var timestamp = new DateTime(2026, 11, 10, 9, 15, 30);

                chromeLog.Append(new ChromeActivityEntry
                {
                    Timestamp = timestamp,
                    EventType = ChromeActivityEventTypes.TabObserved,
                    WindowTitle = "Example Site"
                });

                Assert.True(File.Exists(chromeLog.GetPathForDay(timestamp)));
                Assert.Contains(timestamp.Date, chromeLog.GetAvailableDays());
                var entries = chromeLog.GetEntriesForDay(timestamp);
                Assert.Single(entries);
                Assert.Equal("Example Site", entries[0].WindowTitle);
                Assert.Equal(ChromeActivityEventTypes.TabObserved, entries[0].EventType);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        [Fact]
        public void ChromeActivityReport_CorrelatesForegroundTitleIntervalsAndEscapesHtml()
        {
            var workDay = new DaySummary(new DateTime(2026, 11, 10));
            workDay.Entries.Add(new TimeEntry(
                new DateTime(2026, 11, 10, 10, 0, 0),
                new DateTime(2026, 11, 10, 11, 0, 0)));

            var entries = new[]
            {
                new ChromeActivityEntry
                {
                    Timestamp = new DateTime(2026, 11, 10, 9, 30, 0),
                    EventType = ChromeActivityEventTypes.TabObserved,
                    WindowTitle = "<script>alert(1)</script>"
                },
                new ChromeActivityEntry
                {
                    Timestamp = new DateTime(2026, 11, 10, 10, 30, 0),
                    EventType = ChromeActivityEventTypes.TabChanged,
                    WindowTitle = "Second Site"
                },
                new ChromeActivityEntry
                {
                    Timestamp = new DateTime(2026, 11, 10, 11, 30, 0),
                    EventType = ChromeActivityEventTypes.NewTab,
                    WindowTitle = "New Tab"
                },
                new ChromeActivityEntry
                {
                    Timestamp = new DateTime(2026, 11, 10, 12, 0, 0),
                    EventType = ChromeActivityEventTypes.RecordingStopped
                }
            };

            string html = new ChromeActivityReportGenerator().GenerateReport(entries, new[] { workDay });

            Assert.Contains("<details>", html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("2026-11-10 10:00:00", html);
            Assert.Contains("2026-11-10 11:00:00", html);
            Assert.Contains("Unknown (outside recorded timer work timestamps)", html);
        }
    }
}
