using Microsoft.AspNetCore.Components.Forms;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class CandidateAndLookUpsAndSchedulePapersRequestDto
    {
        public IBrowserFile File { get; set; }

        public long VenueId { get; set; }

        public long SchedulePaperId { get; set; }
    }
}
