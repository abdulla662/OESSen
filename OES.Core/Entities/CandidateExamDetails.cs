using OES.Core.Entities.Schedule;
using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Core.Entities
{
    public class CandidateExamDetails : BaseEntity<long>
    {
        public string? CandidateCode { get; set; }
        public string? CandidateNationalId { get; set; }
        public string? CandidateCenterRegistrationCode { get; set; } // TCID
        public long RegistrationId { get; set; }

        // User Details
        public long UserId { get; set; }
        public string? CandidateDisplayName { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidatePhoneNumber { get; set; }

        // Candidate Papers
        public int CandidatePaperId { get; set; }
        public long CandidateId { get; set; }
        public long PaperFormId { get; set; }
        public DateTime CandidateExamDate { get; set; }

        // Paper Form Details
        public long PaperFormIdActual { get; set; }
        public long OriginalPaperId { get; set; }
        public string? PaperFormName { get; set; }

        // Paper Details
        public int PaperIdActual { get; set; }
        public string? PaperName { get; set; }
        public string? PaperCode { get; set; }
        public Single? PaperDuration { get; set; }
        public int? PaperQuestionsCount { get; set; }
        public int? PaperTotalMarks { get; set; }
        public DateTime? PaperStartDate { get; set; }
        public DateTime? PaperEndDate { get; set; }
        public long ScheduleId { get; set; }
        public string? PaperLanguage { get; set; }
        public bool? PaperAllowInstantResult { get; set; }
        public PaperType? PaperType { get; set; }
        public string? FinalScore { get; set; }
        public decimal? FScore { get; set; }
        public DateTime? GeneratedAt { get; set; }

        // Schedule Details
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

        // Flags
        public int HasNoTrial { get; set; }

        public string? TrackingLogsJSON { get; set; }

        public bool ExamEndedBySystem { get; set; }

        // Aggregated Answer Statistics
        public int TotalQuestionsInDatabase { get; set; }
        public int QuestionsAnsweredCount { get; set; }
        public int QuestionsMarkedForReview { get; set; }
        public int QuestionsVisited { get; set; }
        public double? TotalElapsedTimeInSeconds { get; set; }

        // Timestamps
        public DateTime? CandidateCreatedAt { get; set; }

        // Status Flags
        public bool PaperFormIsDeleted { get; set; }
        public bool PaperIsDeleted { get; set; }

        // Other Fields
        public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.NotApplicable;
        public DateTime? ReviewedAt { get; set; } = null;
        public string? ReviewedBy { get; set; } = null;
        public DateTime? SentToCTRAt { get; set; } = null;
        public string? SentToCTRBy { get; set; } = null;
        public bool SentToCTR { get; set; } = false;
        public long JobId { get; set; } = 0;
        public bool Synced { get; set; } = false;
        public long? VenueId { get; set; }
        public string? VenueCode { get; set; }
        public bool IsDemo { get; set; } = false;

        // Navigation Property
        public virtual Venue? Venue { get; set; }
    }
}
