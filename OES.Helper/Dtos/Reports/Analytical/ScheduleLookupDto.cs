namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ScheduleLookupDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
