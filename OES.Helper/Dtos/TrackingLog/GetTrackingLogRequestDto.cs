namespace OES.Helper.Dtos.TrackingLog
{
    public class GetTrackingLogRequestDto
    {
        public long CandidateId { get; set; }
        public long CandidateExamTrialId { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long ScheduleId { get; set; }
    }
}
