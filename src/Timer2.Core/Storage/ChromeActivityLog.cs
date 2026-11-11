using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public class ChromeActivityLog
    {
        private const string FilePrefix = "chrome-activity-";
        private readonly ILogManager _logManager;
        private readonly object _sync = new object();

        public string DirectoryPath => Path.Combine(_logManager.LogDirectory, "ChromeActivity");

        public ChromeActivityLog(ILogManager logManager)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
        }

        public string GetPathForDay(DateTime date) =>
            Path.Combine(DirectoryPath, $"{FilePrefix}{date:yyyy-MM-dd}.txt");

        public List<DateTime> GetAvailableDays()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return new List<DateTime>();
            }

            return Directory.EnumerateFiles(DirectoryPath, $"{FilePrefix}*.txt")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)
                    ? name.Substring(FilePrefix.Length)
                    : string.Empty)
                .Select(name => DateTime.TryParseExact(
                    name,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime date) ? date : (DateTime?)null)
                .Where(date => date.HasValue)
                .Select(date => date.Value)
                .OrderByDescending(date => date)
                .ToList();
        }

        public List<ChromeActivityEntry> GetEntriesForDay(DateTime date)
        {
            string path = GetPathForDay(date);
            if (!File.Exists(path))
            {
                return new List<ChromeActivityEntry>();
            }

            lock (_sync)
            {
                var entries = new List<ChromeActivityEntry>();
                foreach (string line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    ChromeActivityEntry entry = JsonSerializer.Deserialize<ChromeActivityEntry>(line);
                    if (entry == null)
                    {
                        throw new InvalidDataException($"Invalid Chrome activity entry in {path}.");
                    }
                    entries.Add(entry);
                }

                return entries.OrderBy(entry => entry.Timestamp).ToList();
            }
        }

        public List<ChromeActivityEntry> GetAllEntries()
        {
            return GetAvailableDays()
                .SelectMany(GetEntriesForDay)
                .OrderBy(entry => entry.Timestamp)
                .ToList();
        }

        public void Append(ChromeActivityEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string path = GetPathForDay(entry.Timestamp);
            lock (_sync)
            {
                Directory.CreateDirectory(DirectoryPath);
                File.AppendAllText(
                    path,
                    JsonSerializer.Serialize(entry) + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }
    }
}
