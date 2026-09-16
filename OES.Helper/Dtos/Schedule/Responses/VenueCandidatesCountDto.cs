namespace OES.Helper.Dtos.Schedule.Responses
{
    public class VenueCandidatesCountDto
    {
        public long VenueId { get; set; }

        public string VenueName { get; set; }

        public string VenueCode { get; set; }

        public int CandidatesCount { get; set; }
    }
}
