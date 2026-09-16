namespace OES.Helper.Dtos.Results
{
    public class ExportUnfinishedReviewRequestDto
    {
        public long Id { get; set; }
        public long CandidateId { get; set; }
        public long CandidateExamTrialId { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long ScheduleId { get; set; }
        public DateTime? ExamDate { get; set; }
    }
}
