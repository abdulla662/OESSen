using OES.Helper.Enums;

namespace OES.Helper.Dtos.Paper.Requests
{
    public sealed record AddOrUpdateManualQuestionsRequestDto(List<QuestionSaveDto> SelectedQuestions,
                                                              int QuestionsCount);

    public sealed record QuestionSaveDto(
        long PaperItemBankQuestionId,
        long QuestionMetadataId,
        long ItemBankPointId,
        long DifficultyLevelId,
        long? SectionId,
        PaperQuestionStatus PaperQuestionStatus,
        int SubQuestionsCount
    );
}