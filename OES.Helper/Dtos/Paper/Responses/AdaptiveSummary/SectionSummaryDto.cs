namespace OES.Helper.Dtos.Paper.Responses.AdaptiveSummary
{
    public class SectionSummaryDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; }

        public int SectionOrder { get; set; }

        public List<BlockSummaryDto> Blocks { get; set; }
    }
}
