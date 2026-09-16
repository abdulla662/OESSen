using SharedHelper.Enums;
using SharedHelper.General;

namespace OES.Core.Entities
{
    public class EvaluationSyncJob : BaseEntity<long>
    {
        public Guid JobId { get; set; }
        public string PayloadFilePath { get; set; }
        public DateTime? StartedProcessingAt { get; set; } = DateTimeHelper.Now;
        public DateTime? CompletedAt { get; set; }
        public int TotalAnswers { get; set; }
        public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
