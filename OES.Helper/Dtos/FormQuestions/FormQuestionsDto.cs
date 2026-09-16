namespace OES.Helper.Dtos.FormQuestions
{
    public class FormQuestionsDto
    {
        public long FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string FormCode { get; set; } = string.Empty;
        public string FormDescription { get; set; } = string.Empty;
        public List<QuestionWithScoreDto> Questions { get; set; } = [];
    }
}
