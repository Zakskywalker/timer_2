using System.Collections.Generic;
using System.Threading.Tasks;
using Timer2.Core.Models;

namespace Timer2.Core.Database
{
    public interface IDataConnector
    {
        bool IsConfigured { get; }
        Task<(bool Success, string Message)> TestConnectionAsync();
        Task<(bool Success, string Message)> InitializeSchemaAsync();

        // Dual Table Export Methods
        Task<(int ExportedCount, string Message)> ExportTimerLogsToDbAsync(List<TimeEntry> entries, string tableName = null);
        Task<(int ExportedCount, string Message)> ExportSystemLogsToDbAsync(List<string> logLines, string tableName = null);

        // Retrieve Methods
        Task<List<TimeEntry>> RetrieveTimerLogsFromDbAsync(string tableName = null);
        Task<List<DbSystemLogItem>> RetrieveSystemLogsFromDbAsync(string tableName = null);

        // Compare and User Submit
        Task<(DbComparisonResult Result, string Message)> CompareTimerLogsAsync(List<DaySummary> localDays, string tableName = null);
        Task<(int SubmittedCount, string Message)> SubmitTimerLogsToDbAsync(List<TimeEntry> entriesToSubmit, string tableName = null);

        Task<(int SyncedCount, string Message)> SyncSessionsAsync(List<DaySummary> days);
        Task<(int SyncedCount, string Message)> SyncChangesAsync(List<TimeChangeRecord> changes);
        Task<(bool Success, string Message)> LogEventAsync(string level, string message);
    }
}
