using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Timer2.Core.Models;

namespace Timer2.Core.Export
{
    public class ChromeActivityReportGenerator
    {
        public string GenerateReport(
            IEnumerable<ChromeActivityEntry> chromeEntries,
            IEnumerable<DaySummary> timerDays,
            string themeName = "Dark")
        {
            var entries = chromeEntries.OrderBy(entry => entry.Timestamp).ToList();
            var workPeriods = timerDays
                .SelectMany(day => day.Entries)
                .Where(entry => entry.StopTime.HasValue && entry.StopTime.Value > entry.StartTime)
                .Select(entry => (Start: entry.StartTime, Stop: entry.StopTime.Value))
                .OrderBy(period => period.Start)
                .ToList();

            bool dark = !string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase);
            string background = dark ? "#111827" : "#f8fafc";
            string foreground = dark ? "#e5e7eb" : "#1f2937";
            string muted = dark ? "#9ca3af" : "#64748b";
            string accent = dark ? "#34d399" : "#047857";
            string card = dark ? "#1f2937" : "#ffffff";

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine("<title>Chrome Activity Correlation Report</title>");
            sb.AppendLine($"<style>body{{font:16px Segoe UI, sans-serif;background:{background};color:{foreground};max-width:1000px;margin:2rem auto;padding:0 1rem}}h1{{color:{accent}}}details{{background:{card};border-radius:8px;margin:10px 0;padding:14px}}summary{{cursor:pointer;font-weight:600}}.muted{{color:{muted}}}.unknown{{color:#f59e0b}}li{{margin:.4rem 0}}code{{overflow-wrap:anywhere}}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>Google Chrome Activity &amp; Timer Correlation</h1>");
            sb.AppendLine($"<p class=\"muted\">Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}. Process-only capture records the foreground Chrome window title, not URLs or background-tab changes.</p>");
            sb.AppendLine("<p class=\"muted\">Intervals are correlated only where recorded timer sessions overlap. Other activity is marked unknown.</p>");

            if (entries.Count == 0)
            {
                sb.AppendLine("<p>No Chrome activity was recorded.</p>");
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ChromeActivityEntry entry = entries[i];
                string title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(entry.WindowTitle)
                    ? "(No Chrome window title)"
                    : entry.WindowTitle);
                string timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                sb.AppendLine("<details>");
                sb.AppendLine($"<summary>{WebUtility.HtmlEncode(timestamp)} — {WebUtility.HtmlEncode(FormatEventType(entry.EventType))}: {title}</summary>");

                if (entry.EventType == ChromeActivityEventTypes.Snapshot)
                {
                    sb.AppendLine($"<p>One-off capture at {WebUtility.HtmlEncode(timestamp)}. No duration is inferred.</p>");
                }
                else if (IsActivityStart(entry.EventType))
                {
                    DateTime? stop = FindStopTime(entries, i);
                    if (!stop.HasValue)
                    {
                        sb.AppendLine($"<p class=\"unknown\">{WebUtility.HtmlEncode(timestamp)} → Unknown (no later Chrome transition was captured)</p>");
                    }
                    else
                    {
                        sb.AppendLine($"<p>Chrome observation: {WebUtility.HtmlEncode(timestamp)} → {WebUtility.HtmlEncode(stop.Value.ToString("yyyy-MM-dd HH:mm:ss"))}</p>");
                        var overlaps = workPeriods
                            .Select(period => (
                                Start: period.Start > entry.Timestamp ? period.Start : entry.Timestamp,
                                Stop: period.Stop < stop.Value ? period.Stop : stop.Value))
                            .Where(period => period.Stop > period.Start)
                            .ToList();

                        if (overlaps.Count == 0)
                        {
                            sb.AppendLine("<p class=\"unknown\">Unknown (outside recorded timer work timestamps)</p>");
                        }
                        else
                        {
                            sb.AppendLine("<ul>");
                            foreach (var overlap in overlaps)
                            {
                                sb.AppendLine($"<li>{WebUtility.HtmlEncode(overlap.Start.ToString("yyyy-MM-dd HH:mm:ss"))} → {WebUtility.HtmlEncode(overlap.Stop.ToString("yyyy-MM-dd HH:mm:ss"))}</li>");
                            }
                            sb.AppendLine("</ul>");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"<p class=\"muted\">Event timestamp: {WebUtility.HtmlEncode(timestamp)}.</p>");
                }

                sb.AppendLine("</details>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        public void SaveReportToFile(
            IEnumerable<ChromeActivityEntry> chromeEntries,
            IEnumerable<DaySummary> timerDays,
            string filePath,
            string themeName = "Dark")
        {
            string html = GenerateReport(chromeEntries, timerDays, themeName);
            File.WriteAllText(filePath, html, Encoding.UTF8);
        }

        private static bool IsActivityStart(string eventType) =>
            eventType == ChromeActivityEventTypes.TabObserved ||
            eventType == ChromeActivityEventTypes.TabChanged ||
            eventType == ChromeActivityEventTypes.NewTab ||
            eventType == ChromeActivityEventTypes.ForegroundResumed;

        private static DateTime? FindStopTime(IReadOnlyList<ChromeActivityEntry> entries, int startIndex)
        {
            for (int i = startIndex + 1; i < entries.Count; i++)
            {
                ChromeActivityEntry entry = entries[i];
                if (entry.EventType == ChromeActivityEventTypes.Snapshot)
                {
                    continue;
                }
                if (IsActivityStart(entry.EventType) ||
                    entry.EventType == ChromeActivityEventTypes.FocusLost ||
                    entry.EventType == ChromeActivityEventTypes.RecordingStopped ||
                    entry.EventType == ChromeActivityEventTypes.RecordingStarted)
                {
                    return entry.Timestamp;
                }
            }
            return null;
        }

        private static string FormatEventType(string eventType) =>
            eventType switch
            {
                ChromeActivityEventTypes.TabObserved => "Tab observed",
                ChromeActivityEventTypes.TabChanged => "Tab/window title changed",
                ChromeActivityEventTypes.NewTab => "New tab title observed",
                ChromeActivityEventTypes.ForegroundResumed => "Chrome returned to foreground",
                ChromeActivityEventTypes.FocusLost => "Chrome left foreground",
                ChromeActivityEventTypes.RecordingStarted => "Recording started",
                ChromeActivityEventTypes.RecordingStopped => "Recording stopped",
                ChromeActivityEventTypes.Snapshot => "One-off snapshot",
                _ => eventType
            };
    }
}
