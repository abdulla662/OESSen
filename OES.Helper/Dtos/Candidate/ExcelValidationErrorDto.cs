namespace OES.Helper.Dtos.Candidate
{
    public class ExcelValidationErrorDto
    {
        public int? RowNumber { get; set; }

        public string CandidateIdentifier { get; set; }

        public string FieldName { get; set; }

        public string ErrorMessage { get; set; }

        public string Value { get; set; }
    }
}