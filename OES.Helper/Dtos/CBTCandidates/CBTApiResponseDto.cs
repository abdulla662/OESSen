
namespace OES.Helper.Dtos.CBTCandidates
{
    public class CBTApiResponseDto
    {
        public bool IsSuccess { get; set; }

        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public List<CBTCandidateResponseDto> ResponseDetails { get; set; } = [];
    }
}
