using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;

namespace OES.Helper.Dtos.Paper.Responses
{
    public record GetPaperAutoSectionsWithQuestionsDistributionsResponseDto(List<DistributionSectionResponseDto> PaperAutoSections,
                                                                            List<MixedSelectedQuestionsNodeDto> PaperMixedQuestions);
}
