using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.HotSpotDtos;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public class QuestionMetadataAdditionOrUpdateDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired)), MaxLength(100)]
        public string Code { get; set; }

        public long DifficultyProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        [Range(0.0, 1.0, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ValueMustBeBetween0And1))]
        public decimal Delta { get; set; }

        public bool IsRoot { get; set; }

        public int MaximumAnswerTime { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired)), MaxLength(100)]
        public string Author { get; set; }

        [Required]
        public long QuestionTypeId { get; set; }

        [Required]
        public long QuestionSubjectId { get; set; }

        public long? QuestionCategoryId { get; set; }

        public long? IloId { get; set; }

        public long? QuestionLayoutId { get; set; }

        public long ItemBankId { get; set; }

        public long QuestionsExhaustionCount { get; set; }

        public QuestionStatus QuestionStatus { get; set; }

        public bool ScientificEditorPanelEnabled { get; set; }

        public bool FileManagerEditorPanelEnabled { get; set; }

        public FileUploadSettingsDto FileUploadResponseSettings { get; set; } = new();

        public UploadMode UploadMode { get; set; }

        public List<GetOESGroupDto>? OESGroupDtos { get; set; }

        public HotSpotQuestionSettingsDto HotSpotQuestionSettings { get; set; }
    }
}
