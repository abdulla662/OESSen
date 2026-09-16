
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public sealed record QuestionDeltaValidationResponseDto(
        bool CanProceed,
        bool HasWarnings,
        List<QuestionDeltaRowResultDto> Rows
    );
}
