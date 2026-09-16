namespace OES.Helper.Dtos.EquationTemplate
{
    public class CategoryCalculationDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string OriginalEquation { get; set; }
        public string ProcessedEquation { get; set; }
        public decimal CalculatedValue { get; set; }
        public int QuestionCount { get; set; }
        public int CorrectQuestionCount { get; set; }
        public int IncorrectQuestionCount { get; set; }
        public int UnscoredQuestionCount { get; set; }
        public bool ShowInResults { get; set; } = true;
    }
}