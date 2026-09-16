using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion
{
    public sealed record AddOrUpdateMatchingPairsWithDragDropRequestDto(
        long MetadataParentId,
        List<QuestionDetailsDto> QuestionDetails,
        List<MatchingPairQuestionItemDto> MatchingItems
    );
}
