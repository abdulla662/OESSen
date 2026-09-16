
namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class QuestionSimpleDataDto
    {
        public long Id { get; set; }

        public string Code { get; set; }

        public string Body { get; set; }

        public bool IsSelected { get; set; } = false;

        public long? DifficultyLevelId { get; set; }

        public string DifficultyLevelName { get; set; }

        public int SubQuestionsCount { get; set; } = 1;
    }
}
