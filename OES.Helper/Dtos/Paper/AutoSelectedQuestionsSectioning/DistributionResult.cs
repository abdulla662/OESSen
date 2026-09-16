
using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class DistributionResult
    {
        public Dictionary<string, long> DifficultySelections { get; set; } = [];

        public List<QuestionSimpleDataDto> SelectedQuestions { get; set; } = [];

        public Dictionary<string, List<ComprehensionDistributionItem>> SubQuestionDistributions { get; set; } = [];

        public long TotalSelected =>
            // Auto Comprehension questions: Sum of (SubQuestionsCount * Count) per row
            (SubQuestionDistributions != null && SubQuestionDistributions.Any()
                ? SubQuestionDistributions.Values.SelectMany(v => v).Sum(x => x.SubQuestionsCount * x.Count)
                : DifficultySelections.Values.Sum())
            +
            // Manual questions: For Comprehension passages, sum of SubQuestionsCount, otherwise count 1 per question
            (SelectedQuestions?.Sum(q => q.SubQuestionsCount > 1 ? q.SubQuestionsCount : 1) ?? 0);
    }
}
