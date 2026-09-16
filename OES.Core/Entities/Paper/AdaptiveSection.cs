using SharedHelper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class AdaptiveSection : BaseEntity<long>
    {
        public string Name { get; set; }
        public AdaptivePaperSubtype AdaptivePaperSubtype { get; set; }
        public int Order { get; set; }
        public long StageId { get; set; }
        public bool UnScored { get; set; }
        public long? InstructionSectionTemplateId { get; set; }
        public double TimeInMinutes { get; set; }
        public long DifficultyLevelId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(StageId))]
        public virtual Stage Stage { get; set; }

        [ForeignKey(nameof(DifficultyLevelId))]
        public virtual DifficultyLevel DifficultyLevel { get; set; }

        [ForeignKey(nameof(InstructionSectionTemplateId))]
        public virtual Template InstructionSectionTemplate { get; set; }

        public virtual ICollection<PaperFormBlock> PaperBlocks { get; set; } = [];
    }
}
