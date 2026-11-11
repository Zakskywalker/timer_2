using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Timer2.Core.Calculations;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public class LogIndexManager
    {
        private readonly string _indexFilePath;

        public LogIndexManager(string logDirectory)
        {
            _indexFilePath = Path.Combine(logDirectory, "index.json");
        }

        public LogIndex BuildIndex(List<DaySummary> daySummaries)
        {
            var index = new LogIndex
            {
                LastUpdated = DateTime.Now
            };

            double lifetimeHours = 0;

            foreach (var day in daySummaries.OrderByDescending(d => d.Date))
            {
                var item = new DateIndexItem
                {
                    Date = day.Date,
                    DateString = day.Date.ToString("MM/dd/yyyy"),
                    SessionCount = day.Entries.Count,
                    TotalHours = day.TotalDecimalHours,
                    FormattedTotal = TimeCalculator.FormatTotalLine(day.TotalTime),
                    Sessions = day.Entries
                };
                lifetimeHours += item.TotalHours;
                index.Days.Add(item);
            }

            index.LifetimeTotalHours = Math.Round(lifetimeHours, 2);
            return index;
        }

        public void SaveIndex(LogIndex index)
        {
            try
            {
                var dir = Path.GetDirectoryName(_indexFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(index, options);
                File.WriteAllText(_indexFilePath, json);
            }
            catch
            {
                // Fault tolerant: index file writing shouldn't crash app
            }
        }

        public LogIndex LoadIndex()
        {
            if (File.Exists(_indexFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_indexFilePath);
                    return JsonSerializer.Deserialize<LogIndex>(json) ?? new LogIndex();
                }
                catch
                {
                    return new LogIndex();
                }
            }
            return new LogIndex();
        }
    }
}
