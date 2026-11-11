using System;
using Timer2.Core.Calculations;
using Xunit;

namespace Timer2.Tests
{
    public class ConsoleOutputFilterTests
    {
        [Fact]
        public void FilterMinusTimestamps_RemovesClockTimestampsFromSessionEntries()
        {
            string raw =
                "*Total time in this run: 0 hours, 20 minutes, 39 seconds. (0.34 hrs)\r\n" +
                "Today's total time: 4 hours, 15 minutes, 00 seconds. (4.25 hrs)\r\n" +
                "*Start Time: 11/11/2026 09:00:00 AM\r\n" +
                "● Start=08:30:00 AM, Stop=11:45:00 AM, Duration=3 Hrs. 15 Min. 00 Sec.\r\n" +
                "● Start=01:00:00 PM, Stop=02:00:00 PM, Duration=1 Hrs. 00 Min. 00 Sec.\r\n";

            string filtered = ConsoleOutputFilter.FilterMinusTimestamps(raw);

            // Verified: Timestamps should not be present
            Assert.DoesNotContain("08:30:00 AM", filtered);
            Assert.DoesNotContain("11:45:00 AM", filtered);
            Assert.DoesNotContain("01:00:00 PM", filtered);
            Assert.DoesNotContain("02:00:00 PM", filtered);
            Assert.DoesNotContain("11/11/2026", filtered);

            // Verified: Totals and durations should remain intact
            Assert.Contains("*Total time in this run: 0 hours, 20 minutes, 39 seconds. (0.34 hrs)", filtered);
            Assert.Contains("Today's total time: 4 hours, 15 minutes, 00 seconds. (4.25 hrs)", filtered);
            Assert.Contains("● Session 1: Duration = 3 Hrs. 15 Min. 00 Sec.", filtered);
            Assert.Contains("● Session 2: Duration = 1 Hrs. 00 Min. 00 Sec.", filtered);
            Assert.Contains("*Timer Status: Session Finalized / Closed", filtered);
        }

        [Fact]
        public void FilterMinusTimestamps_HandlesEmptyOrNullInputGracefully()
        {
            Assert.Equal(string.Empty, ConsoleOutputFilter.FilterMinusTimestamps(null));
            Assert.Equal(string.Empty, ConsoleOutputFilter.FilterMinusTimestamps(""));
            Assert.Equal(string.Empty, ConsoleOutputFilter.FilterMinusTimestamps("   \r\n  "));
        }

        [Fact]
        public void FilterMinusTimestamps_PreservesPausedOrIdleIndicator()
        {
            string raw =
                "*Total time in this run: 0 hours, 0 minutes, 0 seconds. (0.00 hrs)\r\n" +
                "Today's total time: 2 hours, 0 minutes, 0 seconds. (2.00 hrs)\r\n" +
                "*Timer Idle / Paused\r\n";

            string filtered = ConsoleOutputFilter.FilterMinusTimestamps(raw);

            Assert.Contains("*Timer Idle / Paused", filtered);
            Assert.Contains("Today's total time: 2 hours, 0 minutes, 0 seconds. (2.00 hrs)", filtered);
        }
    }
}

