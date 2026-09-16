using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Question;
using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.Paper.Requests
{
    public record AddOrUpdatePaperMetadataRequestDto
    {
        public string Name { get; init; }
        public string Description { get; init; }
        public string Code { get; init; }
        public string Abbreviation { get; init; }
        public List<long> SubjectsIds { get; init; }
        public int QuestionsCount { get; init; }
        public float Duration { get; init; }
        public long TotalMarks { get; init; }
        public long LanguageId { get; init; }
        public long OutputFormsCount { get; set; }
        public bool AllowInstantResult { get; init; }
        public bool UsesExcelQuestionsImport { get; init; }
        public int StageCount { get; set; }
        public PaperType Type { get; set; }
        public AdaptivePaperSubtype AdaptiveSubtype { get; set; }
        public bool IsStepPlus { get; set; }
        public QuestionSelectionType QuestionSelectionType { get; set; }
        public QuestionContentType QuestionContentType { get; set; }
        public long? DifficultyProfileId { get; set; }
        public long? TransitionProfileId { get; set; }
        public QuestionDistributionTypeInForm QuestionDistributionTypeInForm { get; set; }
        public QuestionIdsAndItemBankIdsDto UploadedQuestions { get; set; }
        public DPathCalculationMode DPathCalculationMode { get; set; }
        public Dictionary<long, List<decimal>> CategoryStagePaths { get; set; } = [];
        public Dictionary<long, decimal> CategoryFixedDPaths { get; set; } = [];
        public List<long> AdaptiveCategoryExecutionOrder { get; set; } = null;
        public List<GetOESGroupDto>? OESGroupDtos { get; set; }
    }
}