using SharedHelper.Enums;

namespace OES.Core.Entities
{
    public class RealTimeSyncJob : BaseEntity<long>
    {
        public Guid JobId { get; set; }
        public Guid BatchId { get; set; }
        public long ScheduleId { get; set; }
        public string ScheduleName { get; set; }
        public long VenueId { get; set; }
        public string VenueName { get; set; }
        public SyncJobStatus Status { get; set; }
        public string PayloadFilePath { get; set; }
        public DateTime? StartedProcessingAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
        public int CandidateCount { get; set; }
    }
}