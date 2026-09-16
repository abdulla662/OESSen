using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion
{
    public sealed record GetMatchingPairsWithDragDropResponseDto(
        long MetadataParentId,
        IEnumerable<QuestionDetailsDto> QuestionDetails,
        IEnumerable<MatchingPairQuestionItemDto> MatchingItems
    );
}
