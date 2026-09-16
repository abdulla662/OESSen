using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.SyncQuestions;

namespace OES.Helper.Dtos.Paper.Responses
{
    public record GetPaperManualSectionsWithQuestionsResponseDto(
        List<FormDetailsDto> FormNames,
        List<SectionResponseDto> PaperManualSections,
        List<ManuallySelectedQuestionsResponseDto> PaperManualQuestions,
        List<QuestionForPaperDto> QuestionSectionFormDist
    );
}
