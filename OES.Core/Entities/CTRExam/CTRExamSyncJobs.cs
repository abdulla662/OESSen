namespace OES.Core.Entities.CTRExam
{
    public class CTRExamSyncJobs : BaseEntity<long>
    {
        public long VenueId { get; set; }
        public string VenueCode { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long TotalCandidates { get; set; }
        public DateOnly? GenerateResultStartDate { get; set; }
        public DateOnly? GenerateResultEndDate { get; set; }
        public string? FileName { get; set; }
        public DateTime? JobStartTime { get; set; }
        public DateTime? JobEndTime { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string? ErrorCode { get; set; }
    }
}
