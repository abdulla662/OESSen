namespace OES.Helper.Dtos.SearchCriteria.Blazor
{
    public class SearchCriteria
    {
        public string? SearchKey { get; set; }
        public string? SearchIn { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderBy { get; set; }
    }
}
