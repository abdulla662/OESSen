using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidateAndLookUpsAndSchedulePapersResponseDto
    {
        public IFormFile File { get; set; }

        public long VenueId { get; set; }

        public long SchedulePaperId { get; set; }
    }
}
