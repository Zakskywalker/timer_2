using System;
using System.IO;
using Timer2.Core.Models;
using Timer2.Core.Storage;
using Xunit;

namespace Timer2.Tests
{
    public class FaultToleranceTests : IDisposable
    {
        private readonly string _testDir;

        public FaultToleranceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "Timer2_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch { }
        }

        [Fact]
        public void DetectCrashOrUnclosedSession_WithTempCheckpoint_DetectsRecovery()
        {
            var logManager = new LogManager(_testDir, false);
            var startTime = new DateTime(2026, 11, 9, 8, 30, 0);
            logManager.UpdateTempCheckpoint(startTime, TimeSpan.FromMinutes(25), TimeSpan.FromHours(2));

            var recovery = logManager.DetectCrashOrUnclosedSession();

            Assert.True(recovery.HasUnclosedSession);
            Assert.Equal(startTime, recovery.StartTime);
            Assert.Equal("TempCheckpoint", recovery.Source);
        }

        [Fact]
        public void AutoHealCloseLastTimestamp_ClosesAndSavesRecord()
        {
            var logManager = new LogManager(_testDir, false);
            var startTime = new DateTime(2026, 11, 9, 10, 0, 0);
            var stopTime = new DateTime(2026, 11, 9, 11, 30, 0);

            logManager.AutoHealCloseLastTimestamp(startTime, stopTime, out var closedDuration, out var newDayTotal);

            Assert.Equal(TimeSpan.FromMinutes(90), closedDuration);
            Assert.False(File.Exists(logManager.TempLogPath)); // Clears temp checkpoint

            // Verify written in time.txt
            string content = File.ReadAllText(logManager.TimeLogPath);
            Assert.Contains("*AutoHeal Recovery", content);
            Assert.Contains("1.50 hrs", content);
        }

        [Fact]
        public void ReloadTodayRecordings_RefreshesTotalsFromDisk()
        {
            var logManager = new LogManager(_testDir, false);
            var today = DateTime.Today;
            var start = today.AddHours(9);
            var end = today.AddHours(11);

            logManager.AddManualTimePeriod(start, end, "Morning Session", out _, out _);

            var summary = logManager.ReloadTodayRecordings();
            Assert.Single(summary.Entries);
            Assert.Equal(2.0, summary.TotalDecimalHours);
        }
    }
}

