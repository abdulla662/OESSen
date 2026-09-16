using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.HotSpotDtos;

namespace OES.Helper.Dtos.Question
{
    public class GetQuestionMetaDataForQCViewDto
    {
        public string Code { get; set; }
        public string Author { get; set; }
        public string SubjectName { get; set; }
        public string QuestionTypeName { get; set; }
        public string QuestionCategoryName { get; set; }
        public string ProfileName { get; set; }
        public string DifficultyLevel { get; set; }
        public decimal FromDelta { get; set; }
        public decimal ToDelta { get; set; }
        public string ItemBankName { get; set; }
        public long ItemBankNodeId { get; set; }
        public long? ItemBankRootId { get; set; }
        public string IloName { get; set; }
        public decimal DeltaValue { get; set; }
        public int MaximumAnswerTime { get; set; }
        public long QuestionTypeId { get; set; }
        public long? LayoutId { get; set; }
        public string LayoutName { get; set; }
        public bool ScientificEditorPanelEnabled { get; set; }
        public bool FileManagerEditorPanelEnabled { get; set; }
        public long QuestionsExhaustionCount { get; set; }
        public List<QuestionDetailsForQCDto> QuestionDetails { get; set; }
        public long? MaxWords { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
        public FileUploadSettingsDto FileUploadSettings { get; set; }
        public HotSpotQuestionSettingsDto? HotSpotQuestionSettings { get; set; }
    }
}
