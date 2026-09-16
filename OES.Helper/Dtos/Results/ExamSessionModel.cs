using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.Results
{
    public class ExamSessionModel
    {
        public long RegistrationId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string ExamSeries { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public double TotalFinalScore { get; set; }
        public PaperType PaperType { get; set; }
        public AttendantsStatus AttendantStatus { get; set; }
        public GradeStatus Grade { get; set; }
        public bool ReviewedFlag { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewerAccount { get; set; }
        public bool SentToCTRFlag { get; set; }
        public DateTime? SentToCTRDateTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string PaperFormCode { get; set; } = string.Empty;
        public string TotalEquation { get; set; } = string.Empty;
        public bool SentToCTR { get; set; }
        public bool CandidateStartedExam { get; set; }
        public bool CandidateEndedExam { get; set; }
        public ReviewStatus ReviewStatus { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? SentToCTRAt { get; set; }
        public List<EquationCategoryModel> EquationCategories { get; set; } = [];
        public long PaperFormId { get; set; }
    }
}
