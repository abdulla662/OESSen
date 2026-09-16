namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ItemTypeReportDto
    {
        public string FilterLabel { get; set; }
        public List<string> ItemTypes { get; set; } = [];
        public List<ItemTypeBankRowDto> Rows { get; set; } = [];
    }

    public sealed record ItemTypeBankRowDto
    {
        public string ItemBankName { get; set; }
        public Dictionary<string, int> CountsByType { get; set; } = [];
    }
}
