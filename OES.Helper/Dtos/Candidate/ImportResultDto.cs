namespace OES.Helper.Dtos.Candidate
{
    public class ImportResultDto
    {
        public string Message { get; set; }

        public List<ExcelValidationErrorDto> CandidateValidationErrors { get; set; } = [];

        public List<InvalidLookupErrorDto> InvalidLookups { get; set; }
    }
}