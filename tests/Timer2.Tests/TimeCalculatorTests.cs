using System;
using Timer2.Core.Calculations;
using Xunit;

namespace Timer2.Tests
{
    public class TimeCalculatorTests
    {
        [Fact]
        public void CalculateDecimalHours_FourHoursFifteenMinutes_ReturnsFourPointTwentyFive()
        {
            var span = new TimeSpan(4, 15, 0);
            double decimalHours = TimeCalculator.CalculateDecimalHours(span);
            Assert.Equal(4.25, decimalHours);
        }

        [Fact]
        public void CalculateDecimalHours_ThirtyMinutes_ReturnsPointFifty()
        {
            var span = new TimeSpan(0, 30, 0);
            double decimalHours = TimeCalculator.CalculateDecimalHours(span);
            Assert.Equal(0.50, decimalHours);
        }

        [Fact]
        public void FormatDecimalHours_CorrectFormatting()
        {
            var span = new TimeSpan(1, 45, 0);
            string formatted = TimeCalculator.FormatDecimalHours(span);
            Assert.Equal("1.75 hrs.", formatted);
        }

        [Fact]
        public void FormatHP_MatchesRequirement()
        {
            var span = new TimeSpan(4, 15, 0);
            string hpWithUnit = TimeCalculator.FormatHP(span, true);
            string hpNoUnit = TimeCalculator.FormatHP(span, false);

            Assert.Equal("4.25 hrs", hpWithUnit);
            Assert.Equal("4.25", hpNoUnit);
        }

        [Fact]
        public void FormatHM_MatchesRequirement()
        {
            var span = new TimeSpan(4, 15, 23);
            string hmNoSec = TimeCalculator.FormatHM(span, false);
            string hmWithSec = TimeCalculator.FormatHM(span, true);

            Assert.Equal("4 Hrs. 15 Min.", hmNoSec);
            Assert.Equal("4 Hrs. 15 Min. 23 Sec.", hmWithSec);
        }

        [Fact]
        public void FormatRunLine_ContainsDecimalAndComponents()
        {
            var span = new TimeSpan(0, 20, 39);
            string line = TimeCalculator.FormatRunLine(span);
            
            Assert.StartsWith("*Total time in this run: 0 hours, 20 minutes, 39 seconds.", line);
            Assert.Contains("0.34 hrs", line);
        }

        [Fact]
        public void FormatTrayTooltip_ContainsRunAndTotalAndPaused()
        {
            var run = new TimeSpan(5, 36, 16);
            var total = new TimeSpan(5, 36, 16);

            string runningTooltip = TimeCalculator.FormatTrayTooltip(7, run, total, false);
            Assert.Equal("Timer - Run 7: 5:36:16 | Total(Time): 5.60 hrs.", runningTooltip);

            string pausedTooltip = TimeCalculator.FormatTrayTooltip(7, run, total, true);
            Assert.Equal("Timer - Run 7: 5:36:16 | Total(Time): 5.60 hrs.", pausedTooltip);
        }
    }
}
