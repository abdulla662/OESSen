using OES.Helper.Enums;

namespace OES.Helper.Dtos.CandidatesResult
{
    public class UpdateReviewStatusDto
    {
        public long Id { get; set; }
        public ReviewStatus ReviewStatus { get; set; }
        public string ReviewedBy { get; set; } = string.Empty;
    }
}