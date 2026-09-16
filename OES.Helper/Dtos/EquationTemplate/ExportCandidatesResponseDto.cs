namespace OES.Helper.Dtos.EquationTemplate
{
    public class ExportCandidatesResponseDto
    {
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public string ContentType { get; set; }
        public int CandidateCount { get; set; }
        public long FormId { get; set; }
        public long VenueId { get; set; }
        public string VenueCode { get; set; }
        public long PaperId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ExamSeriesCode { get; set; }
        public string CandidateDat { get; set; }
        public string ItemDat { get; set; }
        public string SectDat { get; set; }
        public string ExamDat { get; set; }
        public List<long> RegistrationIds { get; set; } = [];
    }
}