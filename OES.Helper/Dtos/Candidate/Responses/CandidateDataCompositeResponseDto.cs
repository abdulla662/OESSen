using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidateDataCompositeResponseDto
    {
        public IFormFile File { get; set; }

        public bool FromDumb { get; set; }

        public long SchedulePaperId { get; set; }
    }
}
