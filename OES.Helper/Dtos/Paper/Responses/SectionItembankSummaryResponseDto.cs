namespace OES.Helper.Dtos.Paper.Responses
{
    public class SectionItemBankSummaryResponseDto
    {
        public long ItemBankId { get; set; }

        public string ItemBankName { get; set; }

        public List<DifficultyLevelSummaryResponseDto> DifficultyLevelSummaryResponseDtos { get; set; } = [];
    }
}
