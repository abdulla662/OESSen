using OES.Helper.Enums;
using SharedHelper.Enums;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Paper.Responses
{
    public sealed record AddOrUpdatePaperMetadataResponseDto
    {
        public long PaperId { get; set; }
        public string PaperName { get; private init; }
        public long FormId { get; set; }
        public PaperType PaperType { get; private init; }
        public QuestionSelectionType QuestionSelectionType { get; private init; }
        public PaperCreationStatus PaperCreationStatus { get; private init; }
        public int QuestionsCount { get; private init; }
        public float PaperExamDuration { get; private init; }
        public long TotalExamMark { get; private init; }
        public long? DifficultyProfileId { get; private init; }
        public long LanguageId { get; private init; }
        public string LanguageName { get; private init; }
        public long OutputFormsCount { get; private init; }
        public bool AllowInstantResult { get; private init; }
        public bool UsesExcelQuestionsImport { get; private init; }
        public int StageCount { get; private init; }
        public QuestionDistributionTypeInForm QuestionDistributionTypeInForm { get; private init; }
        public DPathCalculationMode DPathCalculationMode { get; private init; }
        public AdaptivePaperSubtype AdaptiveSubtype { get; private init; }
        public bool IsStepPlus { get; private init; }

        public AddOrUpdatePaperMetadataResponseDto() { }

        [JsonConstructor]
        public AddOrUpdatePaperMetadataResponseDto(long paperId,
                                                   string paperName,
                                                   PaperType paperType,
                                                   QuestionSelectionType questionSelectionType,
                                                   PaperCreationStatus paperCreationStatus,
                                                   int questionsCount,
                                                   float paperExamDuration,
                                                   long totalExamMark,
                                                   long? difficultyProfileId,
                                                   long languageId,
                                                   string languageName,
                                                   long outputFormsCount,
                                                   bool allowInstantResult,
                                                   bool usesExcelQuestionsImport,
                                                   int stageCount,
                                                   QuestionDistributionTypeInForm questionDistributionTypeInForm,
                                                   DPathCalculationMode dPathCalculationMode = DPathCalculationMode.ManualFinalScore,
                                                   AdaptivePaperSubtype adaptiveSubtype = AdaptivePaperSubtype.MST,
                                                   bool isStepPlus = false
        )
        {
            PaperId = paperId;
            PaperName = paperName;
            PaperType = paperType;
            QuestionSelectionType = questionSelectionType;
            PaperCreationStatus = paperCreationStatus;
            QuestionsCount = questionsCount;
            PaperExamDuration = paperExamDuration;
            DifficultyProfileId = difficultyProfileId;
            TotalExamMark = totalExamMark;
            LanguageId = languageId;
            LanguageName = languageName;
            OutputFormsCount = outputFormsCount;
            AllowInstantResult = allowInstantResult;
            UsesExcelQuestionsImport = usesExcelQuestionsImport;
            StageCount = stageCount;
            QuestionDistributionTypeInForm = questionDistributionTypeInForm;
            DPathCalculationMode = dPathCalculationMode;
            AdaptiveSubtype = adaptiveSubtype;
            IsStepPlus = isStepPlus;
        }
    }
}