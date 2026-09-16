namespace OES.Helper.Dtos.Candidate
{
    public class VerificationCodeExportDto
    {
        public long Id { get; set; }
        public string RegistrationNumber { get; set; }
        public string NationalId { get; set; }
        public string VenueName { get; set; }
        public string CandidateName { get; set; }
        public string VerificationCode { get; set; }
    }
}
