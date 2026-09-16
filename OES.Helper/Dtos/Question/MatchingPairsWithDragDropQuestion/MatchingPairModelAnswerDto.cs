namespace OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion
{
    public class MatchingPairModelAnswerDto
    {
        public long QuestionItemId { get; set; }
        public List<long> AnswerIds { get; set; } = [];
    }
}
