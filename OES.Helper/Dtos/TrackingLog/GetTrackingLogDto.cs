namespace OES.Helper.Dtos.TrackingLog
{
    public class GetTrackingLogDto
    {
        public long Id { get; set; }
        public long CandidateExamTrialId { get; set; }
        public string SessionLog { get; set; } = "{}";
        public string VenueCode { get; set; } = string.Empty;
    }
}
