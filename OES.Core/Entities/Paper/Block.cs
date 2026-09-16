using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class Block : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public long DifficultyLevelId { get; set; }

        public long DeltaTypeId { get; set; }

        public long QuestionCategoryId { get; set; }

        public long LanguageId { get; set; }

        public bool ConsiderDifficultyLevel { get; set; } = true;


        // Navigational Properties

        [ForeignKey(nameof(DifficultyLevelId))]
        public DifficultyLevel DifficultyLevel { get; set; }

        [ForeignKey(nameof(DeltaTypeId))]
        public DeltaType DeltaType { get; set; }

        [ForeignKey(nameof(QuestionCategoryId))]
        public QuestionCategory QuestionCategory { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        public ICollection<BlockQuestion> Questions { get; set; } = [];

        public ICollection<PaperFormBlock> PaperBlocks { get; set; } = [];

        public virtual ICollection<BlockGroups> BlockGroups { get; set; } = [];
    }
}