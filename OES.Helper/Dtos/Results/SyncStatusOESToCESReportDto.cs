using SharedHelper.Enums;

namespace OES.Helper.Dtos.Results
{
    public class SyncStatusOESToCESReportDto
    {
        public long JobCount { get; set; }
        public long VenueId { get; set; }
        public string VenueName { get; set; }
        public SyncJobStatus Status { get; set; }
        public DateOnly SyncDate { get; set; }
        public string Duration { get; set; }
        public DateTime? LatestRunDate { get; set; }
    }
}
