using OES.Helper.Enums;

namespace OES.Helper.Dtos.CandidatesResult
{
    public class GetCandidateExamDetailsDto
    {
        public long Id { get; set; }

        // Candidate Details
        public long CandidateId { get; set; }
        public long RegistrationId { get; set; }
        public string? CandidateCode { get; set; }
        public string? CandidateNationalId { get; set; }
        public string? CandidateDisplayName { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidatePhoneNumber { get; set; }

        // Paper Details
        public long PaperIdActual { get; set; }
        public long OriginalPaperId { get; set; }
        public string? PaperName { get; set; }
        public string? PaperCode { get; set; }
        public DateTime? PaperStartDate { get; set; }
        public DateTime? PaperEndDate { get; set; }
        public DateTime CandidateExamDate { get; set; }

        // Paper Form Details
        public long PaperFormIdActual { get; set; }
        public string? PaperFormName { get; set; }
        public string? FinalScore { get; set; }

        // Schedule Details
        public long ScheduleIdActual { get; set; }
        public string? ScheduleName { get; set; }
        public string? ScheduleCode { get; set; }

        // Exam Trial Details
        public int? ExamTrialId { get; set; }
        public int TrialNumber { get; set; }
        public bool CandidateStartedExam { get; set; }
        public bool CandidateEndedExam { get; set; }
        public DateTime? ExamTrialStartDate { get; set; }
        public DateTime? ExamTrialEndDate { get; set; }
        public bool CandidateCurrentlyInExam { get; set; }
        public float? TotalExamDuration { get; set; }
        public int TotalExamSessions { get; set; }

        // Answer Statistics
        public int TotalQuestionsInDatabase { get; set; }
        public int QuestionsAnsweredCount { get; set; }
        public int QuestionsMarkedForReview { get; set; }
        public int QuestionsVisited { get; set; }
        public double? TotalElapsedTimeInSeconds { get; set; }

        // Review Status
        public ReviewStatus ReviewStatus { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }

        // CTR Status
        public bool SentToCTR { get; set; }
        public DateTime? SentToCTRAt { get; set; }
        public string? SentToCTRBy { get; set; }

        // Venue
        public string? VenueCode { get; set; }

        // Timestamps
        public DateTime? CandidateCreatedAt { get; set; }
        public string? TrackingLogsJSON { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int TotalSections { get; set; }
    }
}