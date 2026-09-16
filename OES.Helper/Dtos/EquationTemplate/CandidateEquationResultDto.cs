namespace OES.Helper.Dtos.EquationTemplate
{
    public class CandidateEquationResultDto
    {
        public List<CandidateQuestionsAnswersDto> Questions { get; set; }
        public Dictionary<string, CategoryCalculationDto> CategoryCalculations { get; set; }
        public Dictionary<long, List<ItemBankMetricsDto>> CandidateItemBankMetrics { get; set; }
        public decimal FinalScore { get; set; }
        public string TemplateEquation { get; set; }
        public string ProcessedTemplateEquation { get; set; }
        public string TotalEquation { get; set; }
    }
}