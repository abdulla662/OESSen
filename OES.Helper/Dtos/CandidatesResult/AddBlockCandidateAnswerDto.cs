namespace OES.Helper.Dtos.CandidatesResult
{
    public class AddBlockCandidateAnswerDto
    {
        public long CandidateId { get; set; }
        public string RegistrationNumber { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long BlockId { get; set; }
        public long? CandidateExamTrialId { get; set; }
        public long ScheduleId { get; set; }
        public long SchedulePaperId { get; set; }
        public decimal TotalScore { get; set; }
        public int TotalQuestionsCount { get; set; }
        public int CorrectQuestionsCount { get; set; }
        public int IncorrectQuestionsCount { get; set; }
        public bool Synced { get; set; }
        public DateTime? SyncDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long OrganizationId { get; set; }
        public string? OrganizationSignature { get; set; }
        public string VenueCode { get; set; } = string.Empty;
        public long VenueId { get; set; }
    }
}