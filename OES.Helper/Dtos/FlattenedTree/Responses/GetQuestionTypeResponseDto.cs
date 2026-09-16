namespace OES.Helper.Dtos.FlattenedTree.Responses
{
    public class GetQuestionTypeResponseDto
    {
        public long QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public int TypeQuestionsCount { get; set; }
        public List<GetDifficultyLevelResponseDto> DifficultyLevels { get; set; } = [];
    }
}
