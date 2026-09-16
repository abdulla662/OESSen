
namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class SummaryItem
    {
        public string QuestionType { get; set; }

        public long TotalCount { get; set; }

        public Dictionary<string, long> DifficultyBreakdown { get; set; } = [];

        public Dictionary<string, long> SourceItemBanks { get; set; } = [];
    }
}
