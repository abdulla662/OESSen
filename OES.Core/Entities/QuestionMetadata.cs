using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionMetadata : BaseEntity<long>
    {
        [MaxLength(100)]
        public string Code { get; set; }

        [Range(0.0, 1.0)]
        public decimal Delta { get; set; }

        public int MaximumAnswerTime { get; set; } = 0;

        public bool IsRoot { get; set; }

        public long? ParentId { get; set; } = null;

        [Required, MaxLength(100)]
        public string Author { get; set; }

        public QuestionStatus QuestionStatus { get; set; }

        public bool ScientificEditorPanelEnabled { get; set; } = false;

        public bool FileManagerEditorPanelEnabled { get; set; } = false;

        public bool IsReplaced { get; set; } = false;

        public bool Unscored { get; set; }


        // Foreign Keys

        [Required]
        public long QuestionTypeId { get; set; }

        public long? QuestionCategoryId { get; set; }

        [Required]
        public long QuestionsExhaustionCount { get; set; }

        public long CurrentExhaustionCount { get; set; }

        public long? SubjectId { get; set; }

        public long? IloId { get; set; }

        [Required]
        public long ItemBankId { get; set; }

        [Required]
        public long DifficultyProfileId { get; set; }

        [Required]
        public long DifficultyLevelId { get; set; }

        public long? QuestionLayoutId { get; set; }

        public DateTime? LastDeltaUpdatedFromExcel { get; set; }

        public string? DeltaUpdatedFromExcelBy { get; set; }


        // Navigational Properties

        public virtual FileUploadResponseSettings FileUploadResponseSettings { get; set; }

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; }

        [ForeignKey(nameof(QuestionCategoryId))]
        public virtual QuestionCategory QuestionCategory { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; }

        [ForeignKey(nameof(IloId))]
        public virtual ILO Ilo { get; set; }

        [ForeignKey(nameof(ItemBankId))]
        public virtual ItemBank ItemBank { get; set; }

        /// <summary>
        /// Property to force a foreign key constraint of ParentId, in order to perform a self-join relationship
        /// </summary>
        [ForeignKey(nameof(ParentId))]
        public virtual QuestionMetadata ParentSubQuestion { get; set; }

        [ForeignKey(nameof(DifficultyProfileId))]
        public DifficultyProfile DifficultyProfile { get; set; }

        [ForeignKey(nameof(DifficultyLevelId))]
        public DifficultyLevel DifficultyLevel { get; set; }

        [ForeignKey(nameof(QuestionLayoutId))]
        public virtual QuestionLayout QLayout { get; set; }

        public virtual ICollection<QuestionDetails> QuestionDetails { get; set; } = [];

        /// <summary>
        /// In case the type of the question is combo, then here is the list of its children:
        /// </summary>
        public virtual ICollection<QuestionMetadata> SubQuestions { get; set; } = [];

        public virtual ICollection<BlockQuestion>? BlocksQuestions { get; set; } = [];

        public virtual ICollection<ManualPaperItemBankQuestionSection>? ManualPaperItemBankQuestionSections { get; set; } = [];

        public virtual ICollection<QuestionDocLibFile> QuestionDocLibFiles { get; set; } = [];

        public virtual ICollection<GeneratedFormQuestion> FormQuestions { get; set; } = [];

        public virtual ICollection<QuestionGroups> QuestionGroups { get; set; } = [];
    }
}
