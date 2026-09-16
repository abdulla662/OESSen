using SharedHelper.Enums;

namespace OES.Helper.Dtos.CBTCandidates
{
    public class CentersSyncStatusResponseDto
    {
        public string CenterCode { get; set; }
        public string CenterName { get; set; }
        public string Region { get; set; }
        public int CBTReceived { get; set; }
        public int Allocated { get; set; }
        public int Pushed { get; set; }
        public int Acknowledged { get; set; }
        public SyncJobStatus Status { get; set; }
        public DateTime? LastSync { get; set; }
        public DateTime? SyncDate { get; set; }
        public long VenueId { get; set; }
    }
}
