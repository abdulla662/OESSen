using OES.Helper.Enums;

namespace OES.Helper.Dtos.FormQuestions
{
    public class FormQuestionDetailedDto
    {
        public string FormName { get; set; }

        public string FormDescription { get; set; }

        public string FormCode { get; set; }

        public long QuestionId { get; set; }

        public string Code { get; set; }

        public long QuestionTypeId { get; set; }

        public string QuestionType { get; set; }

        public string QuestionTypeDisplay => QuestionType?.ToLocalizedString<QuestionType>();

        public int SubQuestionsCount { get; set; }

        public long QuestionsExhaustionCount { get; set; }

        public long CurrentExhaustionCount { get; set; }

        public long DifficultyProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        public string DifficultyLevelName { get; set; }

        public double? Score { get; set; }

        public long ItembankId { get; set; }

        public string ItemBankName { get; set; }

        public double? Deltavalue { get; set; } = 0.0;

        public PaperQuestionStatus PaperQuestionStatus { get; set; }

        public int UsedInFormsCount { get; set; }
    }
}
