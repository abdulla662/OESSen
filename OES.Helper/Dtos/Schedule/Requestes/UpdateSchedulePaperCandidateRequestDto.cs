namespace OES.Helper.Dtos.Schedule.Requestes
{
    public class UpdateSchedulePaperCandidateRequestDto
    {
        public long SchedulePaperCandidateId { get; set; }
        public DateTime? NewExamDate { get; set; }
        public long NewVenueId { get; set; }
        public string NewCenterCode { get; set; }
        public long RegistrationNumber { get; set; }
        public string OldVenueCode { get; set; }
        public string NewVenueCode { get; set; }
    }
}