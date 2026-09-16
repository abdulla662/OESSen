namespace OES.Helper.Dtos.SyncQuestions
{
    public class QuestionBaseInternal
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public decimal Delta { get; set; }
        public string TypeName { get; set; }
        public bool? IsAutoCorrectable { get; set; }
        public string LayoutName { get; set; }
        public string SubjectName { get; set; }
        public bool ScientificEditorPanelEnabled { get; set; }
        public bool FileManagerEditorPanelEnabled { get; set; }
        public FileUploadSettingsInternal? FileUploadSettings { get; set; }
        public List<long> SubQuestionIds { get; set; }
    }
}
