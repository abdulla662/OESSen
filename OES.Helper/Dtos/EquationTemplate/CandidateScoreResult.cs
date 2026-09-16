namespace OES.Helper.Dtos.EquationTemplate
{
    public class CandidateScoreResult
    {
        public decimal FinalScore { get; set; }
        public Dictionary<string, CategoryCalculationDto> CategoryValues { get; set; }
    }
}