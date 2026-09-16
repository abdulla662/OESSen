using Microsoft.AspNetCore.Components;
using MudBlazor;
using SharedHelper.Enums;
using SharedHelper.General;

namespace OES.Blazor.Pages.SyncStatus
{
    public partial class CenterDetailDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

        [Parameter] public CenterSyncStatus CenterStatus { get; set; } = null!;

        // Mock data for tabs
        private int _mockAverageLatency = 450;
        private int _mockMaxLatency = 1200;
        private int _mockMinLatency = 180;
        private double _mockSuccessRate = 98.5;
        private int _mockTotalErrors = 3;
        private int _mockRetryAttempts = 2;

        private string _searchExamineeString = string.Empty;
        private List<ExamineeRecord> _mockExaminees = [];
        private List<SyncHistoryRecord> _mockSyncHistory = [];
        private List<ErrorLogEntry> _mockErrorLogs = [];

        protected override void OnInitialized()
        {
            GenerateMockData();
        }

        private void GenerateMockData()
        {
            var random = new Random(CenterStatus.CenterCode.GetHashCode());

            // Generate mock examinees
            _mockExaminees = [.. Enumerable.Range(1, CenterStatus.ExpectedExaminees)
                .Select(i => new ExamineeRecord
                {
                    RegistrationId = $"REG-{CenterStatus.CenterCode}-{i:D4}",
                    NationalId = $"{random.Next(10000000, 99999999)}",
                    FullName = $"Examinee {i}",
                    ExamSeriesCode = $"CS-2025-{(char)('A' + random.Next(0, 4))}",
                    Status = i <= CenterStatus.AckReceived ? "Synced" :
                             i <= CenterStatus.PushedToServer ? "Pending Ack" :
                             i <= CenterStatus.ImportedToCentral ? "Pending Push" : "Failed",
                    LastUpdated = DateTimeHelper.Now.AddMinutes(-random.Next(5, 120))
                })];

            // Generate mock history
            _mockSyncHistory = [.. Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var examinees = CenterStatus.ExpectedExaminees + random.Next(-20, 20);
                    var errors = i == 0 && CenterStatus.Status == SyncJobStatus.Failed ? random.Next(5, 15) : random.Next(0, 3);
                    return new SyncHistoryRecord
                    {
                        Date = DateTime.Today.AddDays(-i),
                        Status = i == 0 ? CenterStatus.Status : (errors > 5 ? SyncJobStatus.Failed : errors > 0 ? SyncJobStatus.InProgress : SyncJobStatus.Success),
                        ExamineesProcessed = examinees,
                        Duration = $"{random.Next(8, 15)}m {random.Next(10, 59)}s",
                        Errors = errors,
                        SuccessRate = Math.Round(((examinees - errors) / (double)examinees) * 100, 1)
                    };
                })];

            // Generate mock error logs
            if (CenterStatus.Status == SyncJobStatus.Failed || CenterStatus.Status == SyncJobStatus.InProgress)
            {
                _mockErrorLogs = new List<ErrorLogEntry>
                {
                    new ErrorLogEntry
                    {
                        Timestamp = DateTimeHelper.Now.AddMinutes(-30),
                        Severity = "Error",
                        ErrorCode = "ERR_TIMEOUT_503",
                        Message = "Server connection timeout after 3 retry attempts",
                        StackTrace = "at SyncService.PushToServer()\nat CentralProcessor.DistributeData()\nat SyncManager.RunSync()"
                    },
                    new ErrorLogEntry
                    {
                        Timestamp = DateTimeHelper.Now.AddMinutes(-25),
                        Severity = "Warning",
                        ErrorCode = "WARN_PARTIAL_DATA",
                        Message = "Partial data acknowledged. 5 examinees pending confirmation.",
                        StackTrace = null
                    },
                    new ErrorLogEntry
                    {
                        Timestamp = DateTimeHelper.Now.AddMinutes(-15),
                        Severity = "Info",
                        ErrorCode = "INFO_RETRY_SCHEDULED",
                        Message = "Retry scheduled in 5 minutes",
                        StackTrace = null
                    }
                };
            }
        }

        // Helper methods
        private Color GetStatusColor()
        {
            return CenterStatus.Status switch
            {
                SyncJobStatus.Success => Color.Success,
                SyncJobStatus.InProgress => Color.Info,
                SyncJobStatus.Pending => Color.Warning,
                SyncJobStatus.Failed => Color.Error,
                _ => Color.Default
            };
        }

        private double GetCompletionPercentage()
        {
            if (CenterStatus.ExpectedExaminees == 0) return 0;
            return Math.Round((CenterStatus.AckReceived / (double)CenterStatus.ExpectedExaminees) * 100, 1);
        }

        private Color GetProgressColor()
        {
            var percentage = GetCompletionPercentage();
            return percentage >= 95 ? Color.Success :
                   percentage >= 80 ? Color.Warning :
                   Color.Error;
        }

        private Color GetExamineeStatusColor(string status)
        {
            return status switch
            {
                "Synced" => Color.Success,
                "Pending Ack" => Color.Info,
                "Pending Push" => Color.Warning,
                "Failed" => Color.Error,
                _ => Color.Default
            };
        }

        private Color GetHistoryStatusColor(SyncJobStatus status)
        {
            return status switch
            {
                SyncJobStatus.Success => Color.Success,
                SyncJobStatus.InProgress => Color.Info,
                SyncJobStatus.Pending => Color.Warning,
                SyncJobStatus.Failed => Color.Error,
                _ => Color.Default
            };
        }

        private Color GetSuccessRateColor(double rate)
        {
            return rate >= 95 ? Color.Success :
                   rate >= 80 ? Color.Warning :
                   Color.Error;
        }

        private Color GetLogSeverityColor(string severity)
        {
            return severity switch
            {
                "Error" => Color.Error,
                "Warning" => Color.Warning,
                "Info" => Color.Info,
                _ => Color.Default
            };
        }

        private bool FilterExaminee(ExamineeRecord examinee)
        {
            if (string.IsNullOrWhiteSpace(_searchExamineeString))
                return true;

            return examinee.RegistrationId.Contains(_searchExamineeString, StringComparison.OrdinalIgnoreCase) ||
                   examinee.NationalId.Contains(_searchExamineeString, StringComparison.OrdinalIgnoreCase) ||
                   examinee.FullName.Contains(_searchExamineeString, StringComparison.OrdinalIgnoreCase) ||
                   examinee.ExamSeriesCode.Contains(_searchExamineeString, StringComparison.OrdinalIgnoreCase);
        }

        private void Close() => MudDialog.Close();

        // Data models for dialog tabs
        public class ExamineeRecord
        {
            public string RegistrationId { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string ExamSeriesCode { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime LastUpdated { get; set; }
        }

        public class SyncHistoryRecord
        {
            public DateTime Date { get; set; }
            public SyncJobStatus Status { get; set; }
            public int ExamineesProcessed { get; set; }
            public string Duration { get; set; } = string.Empty;
            public int Errors { get; set; }
            public double SuccessRate { get; set; }
        }

        public class ErrorLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Severity { get; set; } = string.Empty;
            public string ErrorCode { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? StackTrace { get; set; }
        }
    }
}
