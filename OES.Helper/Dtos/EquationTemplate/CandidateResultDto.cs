using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.EquationTemplate
{
    public class CandidateResultDto
    {
        public long RegistrationId { get; set; }
        public string CenterRegistrationCode { get; set; }
        public string ClientCandidateID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string ExamSeriesCode { get; set; }
        public DateTime? ExamStartDate { get; set; }
        public string? FinalScore { get; set; }
        public string PaperFormCode { get; set; }
        public PaperType PaperType { get; set; }
        public List<CategoryCalculationDto>? CategoryEquations { get; set; }
        public long PaperFormId { get; set; }
        public string? TotalEquation { get; set; }
        public string? ReviewerAccount { get; set; }
        public ReviewStatus ReviewStatusEnum { get; set; }
        public bool SentToCTR { get; set; }
        public bool CandidateStartedExam { get; set; }
        public bool CandidateEndedExam { get; set; }
        public int ReviewStatus { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? VenueCode { get; set; }
        public DateTime? SentToCTRAt { get; set; }
        public Dictionary<long, string?>? CategoryIdNameMap { get; set; }
        public decimal ParsedFinalScore { get; set; }
    }
}
