namespace OES.Helper.Dtos.Paper.ImportQuestionsRequestDto
{
    public sealed record AddMultipleQuestionsRequestDto(
        string UserId,
        IEnumerable<AddQuestionRequestDto> AddQuestionRequestDtos,
        bool PaperAllowInstantResult
    );
}