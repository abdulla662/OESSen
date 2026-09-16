namespace OES.Helper.Dtos.CBTCandidates
{
    public class CBTSyncStatusResponseDto
    {
        public int TotalCandidates { get; set; }
        public int ImportedCount { get; set; }
        public int AssignedToPaperCount { get; set; }
        public int NotAssignedToPaperCount { get; set; }
        public DateTime? LastRunTime { get; set; }
        public bool IsManualSync { get; set; }
        public bool IsAutoSync { get; set; }
        public long VenueId { get; set; }
        public string VenueCode { get; set; }
        public string VenueName { get; set; }
        public int TotalVenues { get; set; }
        public double AvgTimeMs { get; set; }
    }
}
