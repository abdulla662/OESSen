using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class MixedSelectedQuestionsNodeDto
    {
        public List<QuestionSimpleDataDto> TotalManualQuestions { get; set; } = [];
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public long QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public string SectionName { get; set; }
        public long CurrentQuestionsCount { get; set; }
        public Dictionary<string, long> DifficultyLevelsBreakdown { get; set; } = [];
        public Dictionary<string, long> AssignedDifficultyLevelsBreakdown { get; set; } = [];
        public Dictionary<string, List<ComprehensionDistributionItem>> SubQuestionDistributions { get; set; } = [];
        public long LanguageId { get; set; }
        public long DifficultyProfileId { get; set; }

        public MixedSelectedQuestionsNodeDto() { }

        public MixedSelectedQuestionsNodeDto(List<QuestionSimpleDataDto>? totalManualQuestions,
                                             long itemBankId,
                                             string itemBankName,
                                             long questionTypeId,
                                             string questionTypeName,
                                             string sectionName,
                                             long currentQuestionsCount,
                                             Dictionary<string, long> difficultyLevelsBreakdown,
                                             Dictionary<string, long> assignedDifficultyLevelsBreakdown,
                                             Dictionary<string, List<ComprehensionDistributionItem>> subQuestionDistributions,
                                             long languageId = 0,
                                             long difficultyProfileId = 0)
        {
            TotalManualQuestions = totalManualQuestions ?? [];
            ItemBankId = itemBankId;
            ItemBankName = itemBankName;
            QuestionTypeId = questionTypeId;
            QuestionTypeName = questionTypeName;
            SectionName = sectionName;
            CurrentQuestionsCount = currentQuestionsCount;
            DifficultyLevelsBreakdown = difficultyLevelsBreakdown;
            AssignedDifficultyLevelsBreakdown = assignedDifficultyLevelsBreakdown;
            SubQuestionDistributions = subQuestionDistributions;
            LanguageId = languageId;
            DifficultyProfileId = difficultyProfileId;
        }
    }
}
