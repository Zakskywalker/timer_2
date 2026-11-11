using System;
using System.Collections.Generic;
using Timer2.Core.Storage;
using Xunit;

namespace Timer2.Tests
{
    public class LogParserTests
    {
        [Fact]
        public void ParseTimeLogLines_StandardLog_ParsesEntriesCorrectly()
        {
            var lines = new List<string>
            {
                "1/13/2024 2:36:16 PM",
                "*Start Time: 1/13/2024 2:36:54 PM",
                "*Stope Time: 1/13/2024 2:37:21 PM",
                "*Total time in this run: 0 hours, 0 minutes, 27 seconds.",
                "Total time is: 0 Hrs. 0 Min. 27 Sec.  or (Rounded up): 0. hrs.",
                "",
                "*Start Time: 1/13/2024 2:37:35 PM",
                "*Stop Time: 1/13/2024 2:41:49 PM",
                "*Total time in this run: 0 hours, 4 minutes, 13 seconds.",
                "Total time is: 0 Hrs. 4 Min. 40 Sec.  or (Rounded up): 0..07 hrs."
            };

            var parser = new LogParser();
            var days = parser.ParseTimeLogLines(lines);

            Assert.Single(days);
            var day = days[0];
            Assert.Equal(new DateTime(2024, 1, 13), day.Date);
            Assert.Equal(2, day.Entries.Count);

            var entry1 = day.Entries[0];
            Assert.Equal(27, entry1.Duration.TotalSeconds);
            Assert.False(entry1.HasWarning);

            var entry2 = day.Entries[1];
            Assert.Equal(254, entry2.Duration.TotalSeconds);
            Assert.False(entry2.HasWarning);

            Assert.Equal(281, day.TotalTime.TotalSeconds);
        }

        [Fact]
        public void ParseTimeLogLines_MissingStopTime_DetectsWarning()
        {
            var lines = new List<string>
            {
                "1/14/2024 10:00:00 AM",
                "*Start Time: 1/14/2024 10:00:00 AM",
                // Notice missing stop time before next start
                "*Start Time: 1/14/2024 11:00:00 AM",
                "*Stop Time: 1/14/2024 12:00:00 PM"
            };

            var parser = new LogParser();
            var days = parser.ParseTimeLogLines(lines);

            Assert.Single(days);
            var day = days[0];
            Assert.Equal(2, day.Entries.Count);

            // First entry should have missing stop warning
            Assert.True(day.Entries[0].HasWarning);
            Assert.Equal("MissingStop", day.Entries[0].WarningType);

            // Second entry is valid
            Assert.False(day.Entries[1].HasWarning);
            Assert.Equal(1.0, day.Entries[1].DecimalHours);
        }

        [Fact]
        public void ParseHistoricalLogs_IfPresent_ParsesAllDaysWithoutError()
        {
            string historicalPath = @"C:\Users\zaksk\.gemini\antigravity\brain\48bf2bd1-3eee-42af-a165-0888e7bf5cf1\scratch\TimerLogs\TimerLogs\time.txt";
            if (System.IO.File.Exists(historicalPath))
            {
                var parser = new LogParser();
                var days = parser.ParseTimeLogFile(historicalPath);

                Assert.NotEmpty(days);
                Assert.True(days.Count > 100); // 8000+ lines contains hundreds of days!
            }
        }
    }
}
