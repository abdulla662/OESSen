using OES.Helper.Enums;

namespace OES.Core.Entities
{
    public class CandidateQuestionsAnswers : BaseEntity<long>
    {
        public long CandidateId { get; set; }
        public string CandidateCode { get; set; } = null!;
        public string CandidateNationalId { get; set; } = null!;
        public string CandidateName { get; set; } = null!;
        public string CandidateEmail { get; set; } = null!;
        public string? CandidatePhoneNumber { get; set; }
        public bool Gender { get; set; }
        public long QuestionId { get; set; }
        public long SectionId { get; set; }
        public int SectionType { get; set; }
        public string SectionName { get; set; } = null!;
        public long? ParentQuestionId { get; set; }
        public bool IsRootQuestion { get; set; }
        public string? QuestionCode { get; set; }
        public string QuestionType { get; set; } = null!;
        public string QuestionSubject { get; set; } = null!;
        public decimal QuestionScore { get; set; }
        public bool IsAutoCorrectable { get; set; }
        public string ModelAnswerIds { get; set; } = null!;
        public long? AnswerId { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerIds { get; set; }
        public string? KeyAnswer { get; set; }
        public string? Response { get; set; }
        public bool Visited { get; set; }
        public bool Answered { get; set; }
        public bool MarkedForReview { get; set; }
        public double? ElapsedTimeInSeconds { get; set; }
        public int TrialNumber { get; set; }
        public long? CandidateExamTrialId { get; set; }
        public long ScheduleId { get; set; }
        public long SchedulePaperId { get; set; }
        public long PaperFormId { get; set; }
        public string PaperFormName { get; set; } = null!;
        public long PaperId { get; set; }
        public string PaperName { get; set; } = null!;
        public DateTime? AnswerCreatedAt { get; set; }
        public DateTime? AnswerLastModifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExamStartDate { get; set; }
        public DateTime? ExamEndDate { get; set; }
        public int TCID { get; set; }
        public decimal MarksObtained { get; set; }
        public int? VenueId { get; set; }
        public string? VenueCode { get; set; }
        public long RegistrationId { get; set; }
        public string ClientCandidateId { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public EvaluationStatus EvaluationStatus { get; set; }
        public DateTime? EvaluationSyncDate { get; set; }
        public Guid? EvaluationSyncJobId { get; set; }
    }
}

