namespace OES.Helper.Dtos.Results
{
    public class VenueCodeGroupDto
    {
        public string VenueCode { get; set; } = string.Empty;
        public string VenueDisplayName { get; set; } = string.Empty;
        public int Allocated { get; set; }
        public int Attended { get; set; }
        public int Absent { get; set; }
        public int TakenSession { get; set; }
        public int UnfinishedSession { get; set; }
        public string Reviewed { get; set; } = string.Empty;
        public bool SentToCTR { get; set; }
        public bool ShowDetails { get; set; }
        public List<AllocationChildDto> Children { get; set; } = [];
    }
}
