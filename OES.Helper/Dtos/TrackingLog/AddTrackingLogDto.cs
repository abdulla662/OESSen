namespace OES.Helper.Dtos.TrackingLog
{
    public class AddTrackingLogDto
    {
        public long CandidateId { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long ScheduleId { get; set; }
        public long CandidateExamTrialId { get; set; }
        public string SessionLog { get; set; } = "{}";
        public string VenueCode { get; set; } = string.Empty;
    }
}
