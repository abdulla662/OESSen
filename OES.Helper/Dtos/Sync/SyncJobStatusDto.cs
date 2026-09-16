using SharedHelper.Enums;

namespace OES.Helper.Dtos.Sync
{
    public class SyncJobStatusDto
    {
        public long Id { get; set; }
        public Guid BatchId { get; set; }
        public long ScheduleId { get; set; }
        public string VenueName { get; set; }
        public string ScheduleName { get; set; }
        public SyncJobStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
        public string PayloadFilePath { get; set; }
        public int CandidateCount { get; set; }
    }
}