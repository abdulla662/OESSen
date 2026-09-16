namespace OES.Helper.Dtos.EquationTemplate
{
    public class CandidateQuestionsAnswersDto
    {
        public long CandidateID { get; set; }
        public string CandidateCode { get; set; } = null!;
        public string ClientCandidateID { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
        public bool Gender { get; set; }
        public string CandidateEmail { get; set; } = null!;
        public string? CandidatePhoneNumber { get; set; }
        public long QuestionId { get; set; }
        public long? ParentQuestionId { get; set; }
        public bool IsRootQuestion { get; set; }
        public string? QuestionCode { get; set; }
        public string QuestionType { get; set; } = null!;
        public string QuestionSubject { get; set; } = null!;
        public decimal QuestionScore { get; set; }
        public bool IsAutoCorrectable { get; set; }
        public long? AnswerId { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerIds { get; set; }
        public bool Visited { get; set; }
        public bool Answered { get; set; }
        public bool MarkedForReview { get; set; }
        public double? ElapsedTimeInSeconds { get; set; }
        public long PaperFormId { get; set; }
        public string PaperFormName { get; set; } = null!;
        public DateTime? AnswerCreatedAt { get; set; }
        public DateTime? AnswerLastModifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }


        // Additional Properties for Equation Processing

        public long? ItemBankId { get; set; }
        public string? ItemBankName { get; set; }
        public int AnswerNumericValue { get; set; }
        public int ModelAnswerNumericValue { get; set; }
        public int CorrectAnswer { get; set; }
        public decimal FinalDeltaValue { get; set; }
        public string? OriginalEquation { get; set; }
        public string? EquationCategoryName { get; set; }
        public string? EquationTemplateName { get; set; }
        public string? TotalEquation { get; set; }
        public string? ProcessedEquation { get; set; }
        public string? ResultWithEquation { get; set; }
        public string? KeyAnswer { get; set; }
        public string? Response { get; set; }
    }
}