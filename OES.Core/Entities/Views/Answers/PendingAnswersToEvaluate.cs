using SharedHelper.Enums;

namespace OES.Core.Entities.Views.Answers
{
    public class PendingAnswersToEvaluate
    {
        public int CandidateExamID { get; set; }
        public long CandidateID { get; set; }
        public long RegistrationId { get; set; }
        public int? VenueId { get; set; }
        public string? VenueCode { get; set; }
        public string? OrganizationSignature { get; set; }
        public long? OrganizationId { get; set; }
        public long EventID { get; set; }
        public string EventName { get; set; } = null!;
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public int PaperID { get; set; }
        public string PaperName { get; set; } = null!;
        public DateTime? ExamAttemptDate { get; set; }
        public int AttemptNumber { get; set; }
        public decimal? MarksObtained { get; set; }
        public bool IsExamCompleted { get; set; }
        public DateTime? ExamEndDate { get; set; }
        public long CandidateAnswerID { get; set; }
        public long QuestionID { get; set; }
        public long? ParentQuestionID { get; set; }
        public string SectionName { get; set; } = null!;
        public string? QuestionText { get; set; }
        public string? ModelAnswer { get; set; }
        public string? TypedAnswerText { get; set; }
        public long? SegmentOrderNumber { get; set; }
        public SegmentQuestionResponseType? SegmentResponseType { get; set; }
        public string? SegmentAudioUrl { get; set; }
        public decimal FullMark { get; set; }
        public long QuestionTypeId { get; set; }
        public string? QuestionTypeName { get; set; }
    }
}
