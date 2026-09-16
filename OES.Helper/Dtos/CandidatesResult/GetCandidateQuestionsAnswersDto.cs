
namespace OES.Helper.Dtos.CandidatesResult
{
    public class GetCandidateQuestionsAnswersDto
    {
        public long CandidateId { get; set; }
        public string CandidateCode { get; set; }
        public string CandidateNationalId { get; set; }
        public string CandidateName { get; set; }
        public bool Gender { get; set; }
        public string CandidateEmail { get; set; }
        public string? CandidatePhoneNumber { get; set; }
        public long QuestionId { get; set; }
        public long? ParentQuestionId { get; set; }
        public bool IsRootQuestion { get; set; }
        public string? QuestionCode { get; set; }
        public string QuestionType { get; set; }
        public string QuestionSubject { get; set; }
        public decimal QuestionScore { get; set; }
        public bool IsAutoCorrectable { get; set; }
        public long? AnswerId { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerIds { get; set; }
        public string? KeyAnswer { get; set; }
        public string? Response { get; set; }
        public bool Visited { get; set; }
        public bool Answered { get; set; }
        public bool MarkedForReview { get; set; }
        public long? CandidateExamTrialId { get; set; }
        public double? ElapsedTimeInSeconds { get; set; }
        public long PaperFormId { get; set; }
        public string PaperFormName { get; set; }
        public DateTime? AnswerCreatedAt { get; set; }
        public DateTime? AnswerLastModifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? VenueId { get; set; }
        public string? VenueCode { get; set; }
        public decimal MarksObtained { get; set; }
        public string ModelAnswerIds { get; set; }
        public long PaperId { get; set; }
        public string PaperName { get; set; }
        public long SectionId { get; set; }
        public string SectionName { get; set; }
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public string ClientCandidateId { get; set; }
        public DateTime? ExamStartDate { get; set; }
        public DateTime? ExamEndDate { get; set; }
        public bool IsCorrect { get; set; }
        public int TCID { get; set; }
    }
}
