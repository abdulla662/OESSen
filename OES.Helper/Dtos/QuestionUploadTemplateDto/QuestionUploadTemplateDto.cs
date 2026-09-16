namespace OES.Helper.Dtos.QuestionUploadTemplateDto
{
    namespace OES.Helper.Dtos.UploadFiles
    {
        public class QuestionUploadTemplateDto
        {
            public long Id { get; set; }

            public string Name { get; set; }

            public string Code { get; set; }

            public long SubjectId { get; set; }

            public long CategoryId { get; set; }

            public long ProfileId { get; set; }

            public long DifficultyLevelId { get; set; }

            public long LanguageId { get; set; }

            public int QuestionsExhaustionCount { get; set; }

            public decimal Delta { get; set; }

            public int? MaximumAnswerTime { get; set; }

            public List<long> SelectedTypeIds { get; set; } = [];

            public long? ItemBankNodeId { get; set; }

            public long? IloNodeId { get; set; }

            public bool FileManagerEditorPanelEnabled { get; set; }

            public bool ScientificEditorPanelEnabled { get; set; }
        }
    }
}

