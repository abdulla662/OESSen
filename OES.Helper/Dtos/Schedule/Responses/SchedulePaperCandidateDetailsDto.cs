namespace OES.Helper.Dtos.Schedule.Responses
{
    public class SchedulePaperCandidateDetailsDto
    {
        public long Id { get; set; }
        public long CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string NationalId { get; set; }
        public long RegistrationNumber { get; set; }
        public string PaperName { get; set; }
        public string ScheduleName { get; set; }
        public string Status { get; set; }
        public DateTime? CandidateExamDate { get; set; }
        public string VenueName { get; set; }
        public string VenueCode { get; set; }
        public string CenterCode { get; set; }
        public long VenueId { get; set; }
        public long SchedulePaperId { get; set; }
        public bool HasAnswers { get; set; }
    }
}