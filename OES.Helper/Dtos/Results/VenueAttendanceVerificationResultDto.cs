namespace OES.Helper.Dtos.Results
{
    public sealed record VenueAttendanceVerificationResultDto
    {
        public string VenueCode { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public int CentralCount { get; set; }
        public int? VenueCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
