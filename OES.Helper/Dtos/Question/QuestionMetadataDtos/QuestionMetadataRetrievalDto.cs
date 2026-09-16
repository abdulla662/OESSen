using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.OESUserGroups;

namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public class QuestionMetadataRetrievalDto
    {
        public long Id { get; set; }

        public string? Code { get; set; }

        public decimal Delta { get; set; }

        public int MaximumAnswerTime { get; set; }

        public bool IsRoot { get; set; }

        public string Author { get; set; }

        public long QuestionTypeId { get; set; }

        public long? QuestionCategoryId { get; set; }

        public long? SubjectId { get; set; }

        public long? RootIloId { get; set; }

        public long? MaxWords { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public SelectedIloNodeFromDialogDto ChildIloDto { get; set; }

        public long? RootItemBankId { get; set; }

        public SelectedItemBankNodeFromDialogDto ChildItemBankDto { get; set; }

        public long DifficultyProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long? QuestionLayoutId { get; set; }

        public long QuestionsExhaustionCount { get; set; }

        public bool ScientificEditorPanelEnabled { get; set; }

        public bool FileManagerEditorPanelEnabled { get; set; }

        public FileUploadSettingsDto? FileUploadSettings { get; set; } = new();

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
