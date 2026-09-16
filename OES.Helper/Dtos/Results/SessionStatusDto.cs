namespace OES.Helper.Dtos.Results
{
    public class SessionStatusDto
    {
        public string ExamSeries { get; set; } = string.Empty;
        public string VenueCode { get; set; } = string.Empty;
        public int TakenSession { get; set; }
        public int UnfinishedSession { get; set; }
        public string Reviewed { get; set; } = string.Empty;
        public int NumSentToCTR { get; set; }
    }
}
