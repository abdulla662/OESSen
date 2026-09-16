using OES.Helper.Dtos.FormQuestions;

namespace OES.Helper.Dtos.Paper.Responses
{
    public sealed record CollectivePaperFormSectionQuestionResponseDto(
        List<FormDetailsDto> Forms,
        List<FormSectionResponseDto> Sections,
        List<ManuallySelectedQuestionsResponseDto> Questions
    );
}
