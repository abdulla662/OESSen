using ReviewStatusEnum = OES.Helper.Enums.ReviewStatus;

namespace OES.Blazor.Models.Results
{
    public class UnfinishedCandidateModel
    {
        public long Id { get; set; }
        public long CandidateId { get; set; }
        public long CandidateExamTrialId { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long ScheduleId { get; set; }
        public DateTime ExamDate { get; set; }
        public long RegistrationId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string ExamSeries { get; set; } = string.Empty;
        public ReviewStatusEnum ReviewStatus { get; set; }
        public string TimeSpentInSession { get; set; } = string.Empty;
        public int ViewedQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int SkippedQuestions { get; set; }
        public string SessionsViewed { get; set; } = string.Empty;
        public string? PaperCode { get; set; }
        public string? VenueCode { get; set; }
        public string? ScheduleName { get; set; }
        public int TotalQuestions { get; set; }
        public int QuestionsAnswered { get; set; }
        public float? TotalExamDuration { get; set; }
        public int TotalExamSessions { get; set; }
        public int TotalSections { get; set; }
    }
}
