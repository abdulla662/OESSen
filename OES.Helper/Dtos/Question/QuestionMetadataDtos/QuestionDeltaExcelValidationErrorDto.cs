
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public sealed record QuestionDeltaExcelValidationErrorDto(
        int? RowNumber,
        string FieldName,
        string ErrorMessage,
        string? QuestionCode = null,
        decimal? DeltaValue = null
    );
}
