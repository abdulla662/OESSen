using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Question.MatchingPairsQuestion
{
    public sealed record GetMatchingPairsResponseDto(
        long MetadataParentId,
        IEnumerable<QuestionDetailsDto> QuestionDetails,
        List<MatchingPairQuestionItemDto> MatchingItems
    );
}
