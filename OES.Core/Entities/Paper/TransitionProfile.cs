using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class TransitionProfile : BaseEntity<long>
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Description { get; set; }

        public long DifficultyProfileId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(DifficultyProfileId))]
        public virtual DifficultyProfile DifficultyProfile { get; set; }

        public virtual ICollection<TransitionLevel> TransitionLevels { get; set; } = [];

        public virtual ICollection<PaperMetadata> PaperMetadata { get; set; } = [];

        public virtual ICollection<TransitionProfileGroups> TransitionProfileGroups { get; set; } = [];
    }
}
