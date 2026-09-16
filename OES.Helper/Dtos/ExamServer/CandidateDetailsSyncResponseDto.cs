namespace OES.Helper.Dtos.ExamServer
{
    public class CandidateDetailsSyncResponseDto
    {
        public long UserId { get; set; }
        public string Code { get; set; }
        public string NationalId { get; set; }
        public string Qualification { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; }
        public string PhotoUrl { get; set; }
        public string SignatureUrl { get; set; }
        public string CenterRegistrationCode { get; set; }
        public DateTime? RegistrationDateTime { get; set; }
        public long? DisabilityId { get; set; }
        public bool HasDisability { get; set; }
    }
}