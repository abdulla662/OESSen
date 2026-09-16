using OES.Helper.Enums;

namespace OES.Core.Entities.Paper.Views
{
    public class CandidateQuestionsWithEquationView
    {
        public long Id { get; set; }

        // ===== Candidate Information =====
        public long CandidateID { get; set; }
        public string? CandidateCode { get; set; }
        public string? ClientCandidateID { get; set; }
        public string? FirstName { get; set; }
        public string MiddleName { get; set; } = null!;  // Always '' in view
        public string LastName { get; set; } = null!;    // Always '.' in view
        public string? CandidateEmail { get; set; }
        public string? CandidatePhoneNumber { get; set; }
        public bool Gender { get; set; }

        // ===== Question Data =====
        public long QuestionId { get; set; }
        public long? ParentQuestionId { get; set; }
        public bool IsRootQuestion { get; set; }
        public long SectionId { get; set; }
        public string? SectionName { get; set; }
        public bool? UnScored { get; set; }
        public int SectionTimeInMinutes { get; set; }
        public string? QuestionCode { get; set; }
        public string QuestionType { get; set; } = null!;
        public string? QuestionSubject { get; set; }
        public decimal QuestionScore { get; set; }
        public bool IsAutoCorrectable { get; set; }

        // ===== Answer Data =====
        public long? AnswerId { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerIds { get; set; }
        public bool Visited { get; set; }
        public bool Answered { get; set; }
        public bool MarkedForReview { get; set; }
        public double? ElapsedTimeInSeconds { get; set; }

        // ===== Paper Data =====
        public long FormId { get; set; }
        public string? FormName { get; set; }
        public long PaperId { get; set; }
        public string? PaperName { get; set; }
        public string? PaperCode { get; set; }

        // ===== Timestamps =====
        public DateTime? AnswerCreatedAt { get; set; }
        public DateTime? AnswerLastModifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        // =====  Exam Dates =====
        public DateTime? ExamStartDate { get; set; }
        public DateTime? ExamEndDate { get; set; }

        // ===== Venue & Registration =====
        public long VenueId { get; set; }
        public string? VenueCode { get; set; }
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public int TCID { get; set; }

        // ===== Scoring =====
        public decimal MarksObtained { get; set; }
        public string? ModelAnswerIds { get; set; }
        public bool IsCorrect { get; set; }

        // ===== Paper Metadata  =====
        public string? ExamSeriesCode { get; set; }
        public int? LanguageId { get; set; }
        public string? ExamLanguage { get; set; }
        public int Attempt { get; set; }
        public long PassingScore { get; set; }
        public string? CleanAnswers { get; set; }
        public string? ModelAnswer { get; set; }

        // ===== Numeric Values =====
        public int ModelAnswerNumericValue { get; set; }
        public int CorrectAnswer { get; set; }

        // ===== Answer Position  =====
        public string? KeyAnswer { get; set; }
        public string? Response { get; set; }

        // ===== Equation & Item Bank Data =====
        public long? ItemBankId { get; set; }
        public string? ItemBankName { get; set; }
        public long? EquationTemplateId { get; set; }
        public long? EquationCategoryId { get; set; }
        public string? EquationTemplateName { get; set; }
        public string? TotalEquation { get; set; }
        public string? EquationCategoryName { get; set; }
        public string? OriginalEquation { get; set; }
        public DateTime? EquationCategoryCreationDate { get; set; }

        // ===== Difficulty Data =====
        public string? DifficultyProfileName { get; set; }
        public string? DifficultyLevelName { get; set; }
        public decimal FinalDeltaValue { get; set; }
        public bool? SentToCTR { get; set; }
        public DateTime? CandidateExamDate { get; set; }
        public EvaluationStatus EvaluationStatus { get; set; }
    }
}