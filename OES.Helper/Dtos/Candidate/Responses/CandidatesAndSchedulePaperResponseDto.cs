using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidatesAndSchedulePaperResponseDto
    {
        public IFormFile File { get; set; } = default!;

        public long SchedulePaperId { get; set; }
    }
}