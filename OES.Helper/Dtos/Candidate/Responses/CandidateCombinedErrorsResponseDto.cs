namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidateCombinedErrorsResponseDto
    {
        public bool? IsExcelValid { get; set; }

        public bool? AreVenuesValid { get; set; }

        public bool? IsDuplicationValid { get; set; }

        public List<ExcelValidationErrorDto> ExcelErrorsDto { get; set; } = [];
    }
}
