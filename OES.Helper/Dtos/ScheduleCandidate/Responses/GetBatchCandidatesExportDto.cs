namespace OES.Helper.Dtos.ScheduleCandidate.Responses
{
    public class GetBatchCandidatesExportDto
    {
        public string CandidateCode { get; set; }
        public string Name { get; set; }
        public string NationalId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Qualification { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public int Gender { get; set; }
        public string RegistrationCenterCode { get; set; }
        public DateTime? RegistrationDateTime { get; set; }
        public long RegistrationNumber { get; set; }
        public string VenueCode { get; set; }
        public DateTime? CandidateExamDate { get; set; }
    }
}
