namespace OES.Helper.Dtos.Paper.Responses
{
    public class StandardAutoPaperSummaryResponseDto
    {
        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public long NumberOfSection { get; set; }

        public long NumberOfQuestion { get; set; }

        public long NumberOfItemBanks { get; set; }

        public List<SectionSummaryResponseDto> SectionSummaryDtos { get; set; } = [];
    }
}
