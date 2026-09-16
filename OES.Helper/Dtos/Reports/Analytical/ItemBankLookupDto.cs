namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ItemBankLookupDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public long? ParentId { get; set; }
    }
}
