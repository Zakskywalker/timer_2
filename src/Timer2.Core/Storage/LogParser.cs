using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public class LogParser
    {
        private static readonly Regex DateHeaderRegex = new Regex(
            @"^\s*(\d{1,2}/\d{1,2}/\d{4})\s+(\d{1,2}:\d{2}(?::\d{2})?\s*(?:AM|PM)?)\s*$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex StartTimeRegex = new Regex(
            @"^\s*\*Start Time:\s*(.+)$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex StopTimeRegex = new Regex(
            @"^\s*\*Sto(?:p|pe) Time:\s*(.+)$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<DaySummary> ParseTimeLogFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<DaySummary>();
            }

            var lines = File.ReadAllLines(filePath);
            return ParseTimeLogLines(lines);
        }

        public List<DaySummary> ParseTimeLogLines(IEnumerable<string> lines)
        {
            var daysMap = new Dictionary<DateTime, DaySummary>();
            DateTime? currentDate = null;
            DateTime? pendingStart = null;
            int runCounter = 1;

            foreach (var rawLine in lines)
            {
                string line = rawLine?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Check for Date Header: e.g. "1/13/2024 2:36:16 PM"
                var dateMatch = DateHeaderRegex.Match(line);
                if (dateMatch.Success && !line.StartsWith("*"))
                {
                    // If we had an open pending start before the new date header, mark it as missing stop time!
                    if (pendingStart.HasValue)
                    {
                        RecordUnclosedSession(daysMap, currentDate, pendingStart.Value, runCounter++);
                        pendingStart = null;
                    }

                    if (DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                        DateTime.TryParse(line, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                    {
                        var parsedDate = dt.Date;
                        if (currentDate != parsedDate)
                        {
                            currentDate = parsedDate;
                            runCounter = 1;
                            if (!daysMap.ContainsKey(parsedDate))
                            {
                                daysMap[parsedDate] = new DaySummary(parsedDate);
                            }
                        }
                    }
                    continue;
                }

                // Check for *Start Time: ...
                var startMatch = StartTimeRegex.Match(line);
                if (startMatch.Success)
                {
                    // If pendingStart already has a value, previous start had missing stop!
                    if (pendingStart.HasValue)
                    {
                        RecordUnclosedSession(daysMap, currentDate, pendingStart.Value, runCounter++);
                        pendingStart = null;
                    }

                    string startStr = startMatch.Groups[1].Value.Trim();
                    if (DateTime.TryParse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDt) ||
                        DateTime.TryParse(startStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out startDt))
                    {
                        pendingStart = startDt;
                        if (!currentDate.HasValue)
                        {
                            currentDate = startDt.Date;
                            if (!daysMap.ContainsKey(currentDate.Value))
                            {
                                daysMap[currentDate.Value] = new DaySummary(currentDate.Value);
                            }
                        }
                    }
                    continue;
                }

                // Check for *Stop Time: or *Stope Time: ...
                var stopMatch = StopTimeRegex.Match(line);
                if (stopMatch.Success)
                {
                    string stopStr = stopMatch.Groups[1].Value.Trim();
                    DateTime stopDt;
                    bool parsed = DateTime.TryParse(stopStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out stopDt) ||
                                 DateTime.TryParse(stopStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out stopDt);

                    if (parsed)
                    {
                        if (pendingStart.HasValue)
                        {
                            DateTime dateKey = currentDate ?? pendingStart.Value.Date;
                            if (!daysMap.TryGetValue(dateKey, out var summary))
                            {
                                summary = new DaySummary(dateKey);
                                daysMap[dateKey] = summary;
                            }

                            var entry = new TimeEntry(pendingStart.Value, stopDt, runCounter++)
                            {
                                Date = dateKey
                            };

                            if (stopDt < pendingStart.Value)
                            {
                                entry.HasWarning = true;
                                entry.WarningType = "NegativeDuration";
                                entry.WarningDetails = $"Stop time ({stopDt:T}) was before Start time ({pendingStart.Value:T}).";
                            }

                            summary.Entries.Add(entry);
                            pendingStart = null;
                        }
                        else
                        {
                            // Stop time without start time
                            DateTime dateKey = currentDate ?? stopDt.Date;
                            if (!daysMap.TryGetValue(dateKey, out var summary))
                            {
                                summary = new DaySummary(dateKey);
                                daysMap[dateKey] = summary;
                            }
                            var entry = new TimeEntry(stopDt, stopDt, runCounter++)
                            {
                                Date = dateKey,
                                HasWarning = true,
                                WarningType = "MissingStart",
                                WarningDetails = $"Found stop time ({stopDt:T}) without preceding start time."
                            };
                            summary.Entries.Add(entry);
                        }
                    }
                    continue;
                }
            }

            // Handle unclosed pending start at end of file
            if (pendingStart.HasValue)
            {
                RecordUnclosedSession(daysMap, currentDate, pendingStart.Value, runCounter++);
            }

            return daysMap.Values.OrderBy(d => d.Date).ToList();
        }

        private void RecordUnclosedSession(Dictionary<DateTime, DaySummary> daysMap, DateTime? currentDate, DateTime start, int runNumber)
        {
            DateTime dateKey = currentDate ?? start.Date;
            if (!daysMap.TryGetValue(dateKey, out var summary))
            {
                summary = new DaySummary(dateKey);
                daysMap[dateKey] = summary;
            }

            var entry = new TimeEntry(start, null, runNumber)
            {
                Date = dateKey,
                HasWarning = true,
                WarningType = "MissingStop",
                WarningDetails = $"Session starting at {start:T} has no recorded stop time."
            };
            summary.Entries.Add(entry);
        }
    }
}
