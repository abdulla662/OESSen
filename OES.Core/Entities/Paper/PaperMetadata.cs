using OES.Core.Entities.Schedule;
using OES.Helper.Enums;
using SharedHelper.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperMetadata : BaseEntity<long>
    {
        [Required, MaxLength(250)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required, MaxLength(100)]
        public string Code { get; set; }

        [MaxLength(100)]
        public string Abbreviation { get; set; }

        [Required]
        public int QuestionsCount { get; set; }

        [Required]
        public float Duration { get; set; }

        public long TotalMarks { get; set; }

        [Required]
        public long LanguageId { get; set; }

        [Required]
        public PaperType Type { get; set; }

        public AdaptivePaperSubtype AdaptiveSubtype { get; set; }

        public bool IsStepPlus { get; set; }

        [Required]
        public QuestionSelectionType QuestionSelectionType { get; set; }

        [Required]
        public long OutputFormsCount { get; set; }

        public bool AllowInstantResult { get; set; }

        public bool UsesExcelQuestionsImport { get; set; }

        public int StageCount { get; set; }

        public long? DifficultyProfileId { get; set; }

        public long? TransitionProfileId { get; set; }

        public PaperCreationStatus PaperCreationStatus { get; set; }

        [Required]
        public QuestionContentType QuestionContentType { get; set; }

        [Required]
        public QuestionDistributionTypeInForm QuestionDistributionTypeInForm { get; set; }

        public AvailabilityStatus PaperStatus { get; set; }

        public DPathCalculationMode DPathCalculationMode { get; set; }

        public string? AdaptiveCategoryExecutionOrder { get; set; }

        [MaxLength(100)]
        public string? LockSessionId { get; set; }

        public DateTime? LockExpiresAt { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(DifficultyProfileId))]
        public virtual DifficultyProfile DifficultyProfile { get; set; }

        [ForeignKey(nameof(TransitionProfileId))]
        public virtual TransitionProfile TransitionProfile { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }

        public ICollection<PaperSubject> Subjects { get; set; }

        public virtual MarkingScheme MarkingScheme { get; set; }

        public virtual ICollection<PaperStageCategoryDecisionPath> PaperStageCategoryDecisionPaths { get; set; }

        public virtual ICollection<PaperFormBlock> PaperFormBlocks { get; set; } = [];

        public virtual ICollection<StandardSection> Sections { get; set; } = [];

        public virtual ICollection<PaperItemBankPoint> ItemBanksPoints { get; set; } = [];

        public virtual ICollection<SchedulePaper> SchedulePapers { get; set; } = [];

        public virtual ICollection<PaperForm> Forms { get; set; } = [];

        public virtual ICollection<PaperGroups> PaperGroups { get; set; } = [];
    }
}