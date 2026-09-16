namespace OES.Helper.Dtos.Candidate
{
    public class InvalidLookupErrorDto
    {
        public string CandidateEmail { get; set; }

        public string ExcelColumnHeader { get; set; }

        public string InvalidLookupValue { get; set; }

        public int OriginalRowNumber { get; set; }

        public string Reason { get; set; }
    }
}