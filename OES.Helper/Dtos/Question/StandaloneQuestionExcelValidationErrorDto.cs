namespace OES.Helper.Dtos.Question
{
    public sealed record StandaloneQuestionExcelValidationErrorDto(
        int? RowNumber,
        string FieldName,
        string ErrorMessage,
        string? QuestionCode = null
    );
}
