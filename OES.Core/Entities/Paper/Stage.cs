using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class Stage : BaseEntity<long>
    {
        public string Name { get; set; }

        public string? RenderedPartName { get; set; }

        public long? FormId { get; set; }

        public int Order { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        [Required]
        public double TimeInMinutes { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(FormId))]
        public PaperForm Form { get; set; }

        public ICollection<AdaptiveSection> AdaptiveSections { get; set; } = [];

        [ForeignKey(nameof(InstructionSectionTemplateId))]
        public virtual Template InstructionSectionTemplate { get; set; }

        public ICollection<PaperStageCategoryDecisionPath> CategoryDecisionPaths { get; set; } = [];
    }
}
