namespace OES.Core.Entities.Schedule.Views
{
    public class SchedulePaperAllocationView
    {
        public long VenueId { get; set; }

        public long SchedulePaperId { get; set; }

        public long OrganizationId { get; set; }

        public string VenueName { get; set; }

        public int CandidateCount { get; set; }
    }
}
