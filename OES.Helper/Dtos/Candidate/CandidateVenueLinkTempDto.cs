namespace OES.Helper.Dtos.Candidate
{
    public class CandidateVenueLinkTempDto
    {
        public string CandidateEmail { get; set; }

        public string VenueCode { get; set; }

        public long VenueId { get; set; }

        public string RegistrationNumber { get; set; }

        public DateTime? CandidateExamDate { get; set; }
    }
}