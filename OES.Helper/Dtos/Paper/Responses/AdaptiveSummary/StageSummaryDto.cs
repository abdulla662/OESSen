namespace OES.Helper.Dtos.Paper.Responses.AdaptiveSummary
{
    public class StageSummaryDto
    {
        public long StageId { get; set; }

        public string StageName { get; set; }

        public int StageOrder { get; set; }

        public decimal StageTime { get; set; }

        public List<SectionSummaryDto> Sections { get; set; }
    }
}
