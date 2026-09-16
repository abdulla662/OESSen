using OES.Helper.Enums;

namespace OES.Helper.Dtos.Results
{
    public class SyncStatusCESToOESReportDto
    {
        public string VenueCode { get; set; }
        public long CandidateCount { get; set; }
        public int Duration { get; set; }
        public SyncStatusReport Status { get; set; }
        public DateTime? LatestRunTime { get; set; }
    }
}
