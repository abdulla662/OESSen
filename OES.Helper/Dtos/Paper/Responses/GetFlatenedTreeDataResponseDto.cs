namespace OES.Helper.Dtos.Paper.Responses
{
    public class GetFlatenedTreeDataResponseDto
    {
        public int NodeNumber { get; set; }

        public Dictionary<string, int> QuestionTypeCounts { get; set; }

        public Dictionary<string, Dictionary<string, int>> QuestionDifficultyBreakdown { get; set; }
    }
}
