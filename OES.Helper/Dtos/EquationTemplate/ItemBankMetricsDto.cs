namespace OES.Helper.Dtos.EquationTemplate
{
    public class ItemBankMetricsDto
    {
        public int TrialNumber { get; set; }
        public long RegistrationId { get; set; }
        public long EquationTemplateId { get; set; }
        public long FormId { get; set; }
        public long CandidateID { get; set; }
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