namespace OES.Helper.Dtos.Paper.Responses
{
    public class SectionSummaryResponseDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; }

        public int OrderId { get; set; }

        public long NumberOfQuestion { get; set; } // Related to each section

        public List<SectionItemBankSummaryResponseDto> SectionItemBankSummaryResponseDtos { get; set; } = [];
    }
}
