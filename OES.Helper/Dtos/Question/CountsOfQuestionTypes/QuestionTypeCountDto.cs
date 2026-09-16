namespace OES.Helper.Dtos.Question.CountsOfQuestionTypes
{
    public class QuestionTypeCountDto
    {
        public string QuestionType { get; set; }

        public List<DifficultyCountDto> Difficulties { get; set; }
    }
}
