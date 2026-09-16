
namespace OES.Core.Entities.Paper.Views
{
    public class FlattenedTreeNodeView
    {
        public long ItemBankPointId { get; set; }
        public long PaperId { get; set; }
        public bool AllowInstantResultPaper { get; set; }
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public long QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public bool IsAutoCorrectableQuestion { get; set; }
        public int QuestionTypeCount { get; set; }
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public int QuestionCount { get; set; }
        public int SubQuestionCount { get; set; }
    }
}
