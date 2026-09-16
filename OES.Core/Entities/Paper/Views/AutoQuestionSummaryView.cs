namespace OES.Core.Entities.Paper.Views
{
    public class AutoQuestionSummaryView
    {
        public long PaperId { get; set; }
        public string PaperName { get; set; }
        public long SectionId { get; set; }
        public string SectionName { get; set; }
        public int SectionOrderId { get; set; }
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public long QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public int QuestionsCount { get; set; }
    }
}
