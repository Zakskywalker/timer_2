using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Timer2.Core.Calculations
{
    /// <summary>
    /// Utility for filtering timer console output before presenting it in the shutdown alert modal,
    /// removing specific time-of-day clock timestamps while preserving run totals and session durations.
    /// </summary>
    public static class ConsoleOutputFilter
    {
        /// <summary>
        /// Filters raw console text by removing clock timestamps
        /// (e.g. Start=08:30:00 AM, Stop=11:45:00 AM, and *Start Time: ...),
        /// preserving totals and clean session durations.
        /// </summary>
        public static string FilterMinusTimestamps(string rawConsole)
        {
            if (string.IsNullOrWhiteSpace(rawConsole))
                return string.Empty;

            var lines = rawConsole.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            int sessionIndex = 1;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // Handle session entry lines: ● Start=08:30:00 AM, Stop=11:45:00 AM, Duration=3 Hrs. 15 Min.
                if (line.StartsWith("●") || line.Contains("Duration=") || (line.Contains("Start=") && line.Contains("Stop=")))
                {
                    var durMatch = Regex.Match(line, @"Duration\s*=\s*(.+)$", RegexOptions.IgnoreCase);
                    if (durMatch.Success)
                    {
                        string durVal = durMatch.Groups[1].Value.Trim();
                        result.Add($"● Session {sessionIndex++}: Duration = {durVal}");
                        continue;
                    }
                }

                // Handle live start time indicator: *Start Time: 11/11/2026 09:00:00 AM
                if (line.StartsWith("*Start Time:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("*Timer Status: Session Finalized / Closed");
                    continue;
                }

                // General run and day total summary lines
                result.Add(line);
            }

            return string.Join(Environment.NewLine, result);
        }
    }
}
