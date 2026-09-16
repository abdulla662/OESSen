namespace OES.Core.Entities
{
    public class QuestionUploadTemplate : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public long SubjectId { get; set; }

        public long CategoryId { get; set; }

        public long ProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long LanguageId { get; set; }

        public int QuestionsExhaustionCount { get; set; }

        public double Delta { get; set; }

        public int? MaximumAnswerTime { get; set; }

        public string SelectedTypeIdsJson { get; set; }

        public long? ItemBankNodeId { get; set; }

        public long? IloNodeId { get; set; }

        public bool FileManagerEditorPanelEnabled { get; set; }

        public bool ScientificEditorPanelEnabled { get; set; }
    }
}