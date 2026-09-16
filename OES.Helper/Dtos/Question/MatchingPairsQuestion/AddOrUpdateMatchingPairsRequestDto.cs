using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Question.MatchingPairsQuestion
{
    public sealed record AddOrUpdateMatchingPairsRequestDto(
        long MetadataParentId,
        List<QuestionDetailsDto> QuestionDetails,
        List<MatchingPairQuestionItemDto> MatchingItems
    );
}
