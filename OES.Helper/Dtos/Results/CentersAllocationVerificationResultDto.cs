namespace OES.Helper.Dtos.Results
{
    public sealed record CentersAllocationVerificationResultDto
    {
        public string VenueCode { get; set; } = "";
        public string VenueName { get; set; } = "";
        public int SyncedCount { get; set; }
        public int NotSyncedCount { get; set; }
        public int TotalExpected { get; set; }
        public int? LocalCount { get; set; }
        public string Status { get; set; } = "";
        public string? ErrorMessage { get; set; }
    }
}
