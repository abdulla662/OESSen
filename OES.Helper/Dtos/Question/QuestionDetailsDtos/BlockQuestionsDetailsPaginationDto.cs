using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class BlockQuestionsDetailsPaginationDto
    {
       public long Id { get; set; }
        public string Code { get; set; }
        public string Subject { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string ItemBank { get; set; }
        public string DifficultyProfile { get; set; }
        public string DifficultyLevel { get; set; }
        public string Body { get; set; }
        public string Language { get; set; }
    }
}
