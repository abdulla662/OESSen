namespace OES.Helper.Dtos.Paper.Responses
{
    public class StandardManualPaperSummaryResponseDto
    {
        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public long NumberOfSection { get; set; }

        public long NumberOfQuestion { get; set; }

        public long NumberOfItemBanks { get; set; }

        public List<FormSummaryResponseDto> FormSummaryResponseDtos { get; set; } = [];
    }
}
