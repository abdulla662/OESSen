namespace OES.Helper.Dtos.Paper.Responses.AdaptiveSummary
{
    public class PaperStageSummaryResponseDto
    {
        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public int NumberOfQuestion { get; set; }

        public int NumberOfSection { get; set; }

        public int NumberOfStages { get; set; }

        public List<StageSummaryDto> Stages { get; set; }
    }
}
