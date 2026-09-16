
namespace OES.Helper.Dtos.EquationTemplate
{
    public class ExportResultDto
    {
        public long FormId { get; set; }
        public string FormName { get; set; }
        public long VenueId { get; set; }
        public string VenueName { get; set; }
        public string VenueCode { get; set; }
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public int CandidateCount { get; set; }
        public long SyncJobId { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsSynced { get; set; }
        public bool IsSyncing { get; set; }
        public string ErrorMessage { get; set; }
        public ExportCandidatesRequestDto Request { get; set; }
        public List<long> RegistrationIds { get; set; } = [];
    }
}