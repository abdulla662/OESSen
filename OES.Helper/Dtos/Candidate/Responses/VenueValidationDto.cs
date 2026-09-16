namespace OES.Helper.Dtos.Candidate.Responses
{
    public class VenueValidationDto
    {
        public long VenueId { get; set; }

        public string VenueCode { get; set; }

        public bool IsExistingInDb { get; set; } = false;

        public bool IsLinkedToSchedule { get; set; } = false;
    }
}
