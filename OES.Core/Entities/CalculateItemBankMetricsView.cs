namespace OES.Core.Entities
{
    /// <summary>
    /// DTO for item bank metrics data (NC, ND, NT, QC values).
    /// Note: This was previously mapped to vw_calculateitembankmetrics database view,
    /// but metrics are now calculated in memory from CandidateQuestionsWithEquationView data
    /// for better performance. This class is kept as a DTO for the calculation results.
    /// </summary>
    public class CalculateItemBankMetricsView
    {
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public long EquationTemplateId { get; set; }
        public long FormId { get; set; }
        public long CandidateID { get; set; }
        public DateOnly? ExamStartDate { get; set; }
        public long? ItemBankId { get; set; }
        public string? ItemBankName { get; set; }
        public int NC { get; set; }
        public double ND { get; set; }
        public double NT { get; set; }
        public int QC { get; set; }
        public string? FirstName { get; set; }
        public string? CandidateCode { get; set; }
        public bool Gender { get; set; }
    }
}
