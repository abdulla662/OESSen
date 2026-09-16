namespace OES.Helper.Dtos.Paper.Responses
{
    public sealed record FormSummaryResponseDto
    {
        public long FormId { get; set; }

        public string FormName { get; set; }

        public List<SectionSummaryResponseDto> SectionSummaryDtos { get; set; } = [];
    }
}
