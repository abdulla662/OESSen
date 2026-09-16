namespace OES.Helper.Dtos.Schedule.Responses
{
    public sealed record GetSchedulePaperAllocationResponseDto
    {
        public long VenueId { get; set; }

        public long SchedulePaperId { get; set; }

        public string VenueName { get; set; }

        public int CandidateCount { get; set; }
    }
}
