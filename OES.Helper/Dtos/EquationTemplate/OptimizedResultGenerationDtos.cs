using SharedHelper.Enums;

namespace OES.Helper.Dtos.EquationTemplate
{
    /// <summary>
    /// Lightweight projection of candidate question data for optimized bulk processing.
    /// Contains only the fields actually used in result generation.
    /// </summary>
    public class OptimizedCandidateQuestionData
    {
        // Candidate identification
        public long CandidateID { get; set; }
        public string CandidateCode { get; set; } = string.Empty;
        public string ClientCandidateID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = ".";
        public bool Gender { get; set; }
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public int TCID { get; set; }

        // Form/Venue grouping
        public long FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public long VenueId { get; set; }
        public string VenueCode { get; set; } = string.Empty;
        public long PaperId { get; set; }
        public string PaperName { get; set; } = string.Empty;

        // Question/Answer data
        public long QuestionId { get; set; }
        public string QuestionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public long SectionId { get; set; }
        public bool? UnScored { get; set; }
        public int SectionTimeInMinutes { get; set; }
        public bool IsCorrect { get; set; }
        public bool Answered { get; set; }
        public bool Visited { get; set; }
        public decimal FinalDeltaValue { get; set; }
        public double? ElapsedTimeInSeconds { get; set; }
        public string? KeyAnswer { get; set; }
        public string? Response { get; set; }

        // Equation data
        public long? ItemBankId { get; set; }
        public string? ItemBankName { get; set; }
        public long? EquationTemplateId { get; set; }
        public long? EquationCategoryId { get; set; }
        public string? EquationCategoryName { get; set; }
        public string? OriginalEquation { get; set; }
        public DateTime? EquationCategoryCreationDate { get; set; }

        // Exam metadata
        public DateTime? ExamStartDate { get; set; }
        public string? ExamSeriesCode { get; set; }
        public string? ExamLanguage { get; set; }
        public long PassingScore { get; set; }
        public long ScheduleId { get; set; }
    }

    /// <summary>
    /// Lightweight projection of item bank metrics for optimized bulk processing.
    /// Note: ItemBankId is nullable because the database view can return NULL for it.
    /// </summary>
    public class OptimizedItemBankMetrics
    {
        public long FormId { get; set; }
        public long EquationTemplateId { get; set; }
        public long CandidateID { get; set; }
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public long? ItemBankId { get; set; }
        public string ItemBankName { get; set; } = string.Empty;
        public int NC { get; set; }
        public double ND { get; set; }
        public double NT { get; set; }
        public string CandidateCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public DateOnly? ExamStartDate { get; set; }
    }

    /// <summary>
    /// Internal class to hold all fetched data for processing.
    /// </summary>
    public class OptimizedResultData
    {
        public List<OptimizedCandidateQuestionData> Questions { get; set; } = [];
        public List<OptimizedItemBankMetrics> Metrics { get; set; } = [];
        public Dictionary<long, string> CategoryIdToName { get; set; } = [];
        public Dictionary<long, EquationTemplateData> Templates { get; set; } = [];
        public List<BlockCandidateAnswerData> Blocks { get; set; } = [];
        public Dictionary<long, string> CategoryNames { get; set; } = [];

        public Dictionary<long, string> EquationCategoryLookup { get; set; }

        /// <summary>
        /// Existing adaptive scores from CandidateExamDetails.FinalScore column.
        /// These are CES-synced scores with QuestionCategory.Id as keys (e.g., {6: 53.13, 7: 50.67}).
        /// Used for PART 3 of GenerateSectionFile in adaptive papers.
        /// Structure: FormId -> (CandidateId, RegistrationId) -> (QuestionCategory.Id -> Score)
        /// </summary>
        public Dictionary<long, Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>> ExistingAdaptiveScoresByForm { get; set; } = [];
    }

    /// <summary>
    /// Equation template data for in-memory processing.
    /// </summary>
    public class EquationTemplateData
    {
        public long TemplateId { get; set; }
        public string TotalEquation { get; set; } = string.Empty;
        public PaperType PaperType { get; set; }
        public AdaptivePaperSubtype PaperSubtype { get; set; }
        public Dictionary<string, (string Equation, bool ShowInResults)> CategoryEquations { get; set; } = [];
    }

    /// <summary>
    /// Block candidate answer data for adaptive papers.
    /// </summary>
    public class BlockCandidateAnswerData
    {
        public long PaperFormId { get; set; }
        public long CandidateId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public long BlockId { get; set; }
        public string BlockCode { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public int CorrectQuestionsCount { get; set; }
        public int IncorrectQuestionsCount { get; set; }
        public int TotalQuestionsCount { get; set; }
    }

    /// <summary>
    /// Grouped data for a single form/venue combination.
    /// </summary>
    public class FormVenueCombinationData
    {
        public long FormId { get; set; }
        public long VenueId { get; set; }
        public string VenueCode { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public long PaperId { get; set; }
        public string PaperName { get; set; } = string.Empty;
        public List<OptimizedCandidateQuestionData> Questions { get; set; } = [];
        public Dictionary<(long CandidateID, int TrialNumber, long RegistrationId), List<OptimizedItemBankMetrics>>? MetricsByCandidate { get; set; }
        public Dictionary<(long CandidateId, string RegistrationNumber), List<BlockCandidateAnswerData>>? BlocksByCandidate { get; set; }
        public EquationTemplateData? Template { get; set; }
    }

    /// <summary>
    /// Score update data for bulk update operation.
    /// </summary>
    public class CandidateScoreUpdate
    {
        public long CandidateId { get; set; }
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public long FormId { get; set; }
        public string FinalScoreJson { get; set; } = string.Empty;
        public decimal EquationFinalScore { get; set; }
        public AdaptivePaperSubtype PaperSubtype { get; set; }
        public Dictionary<long, decimal> AdaptiveCategoryScores { get; set; }
        public PaperType PaperType { get; set; }
        public Dictionary<long, string> CategoryIdToNameLookup { get; set; } = [];
    }
}
