using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class DifficultyLevel : BaseEntity<long>
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Range(0.0, 1.0)]
        public decimal FromDelta { get; set; }

        [Range(0.0, 1.0)]
        public decimal ToDelta { get; set; }

        public long DifficultyProfileId { get; set; }

        public long DeltaTypeId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(DifficultyProfileId))]
        public virtual DifficultyProfile DifficultyProfile { get; set; }

        [ForeignKey(nameof(DeltaTypeId))]
        public virtual DeltaType DeltaType { get; set; }

        public virtual ICollection<Block> Blocks { get; set; } = [];

        public virtual ICollection<AutoPaperItemBankQuestionSection> ItemBankPointsQuestions { get; set; }

        public virtual ICollection<TransitionLevel> TransitionLevels { get; set; } = [];
    }
}
