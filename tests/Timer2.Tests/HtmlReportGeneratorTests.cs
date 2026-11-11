using System;
using System.Collections.Generic;
using Timer2.Core.Export;
using Timer2.Core.Models;
using Xunit;

namespace Timer2.Tests
{
    public class HtmlReportGeneratorTests
    {
        [Fact]
        public void GenerateReport_DarkTheme_ContainsEssentialControls()
        {
            var day = new DaySummary(new DateTime(2026, 11, 9));
            day.Entries.Add(new TimeEntry(
                new DateTime(2026, 11, 9, 9, 0, 0),
                new DateTime(2026, 11, 9, 13, 15, 0),
                1));

            var generator = new HtmlReportGenerator();
            string html = generator.GenerateReport(new List<DaySummary> { day }, "Dark");

            Assert.Contains("Time Recorder — Master Log Report", html);
            Assert.Contains("id=\"searchInput\"", html);
            Assert.Contains("id=\"startDate\"", html);
            Assert.Contains("id=\"endDate\"", html);
            Assert.Contains("id=\"warningBanner\"", html);
            Assert.Contains("id=\"btnOmitWarnings\"", html);
            Assert.Contains("class=\"sticky-header\"", html);
            Assert.Contains("id=\"totalPrimary\"", html);
            Assert.Contains("id=\"totalDecimal\"", html);
            Assert.Contains("4.25", html); // 4 hours 15 mins
        }

        [Fact]
        public void GenerateReport_WithWarnings_EmitsWarningBadges()
        {
            var day = new DaySummary(new DateTime(2026, 11, 9));
            var incompleteEntry = new TimeEntry(
                new DateTime(2026, 11, 9, 14, 0, 0),
                null,
                1);
            day.Entries.Add(incompleteEntry);

            var generator = new HtmlReportGenerator();
            string html = generator.GenerateReport(new List<DaySummary> { day }, "Light");

            Assert.Contains("badge-warning", html);
            Assert.Contains("hasWarning", html);
        }
    }
}
