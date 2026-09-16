using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class DifficultyProfile : BaseEntity<long>
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Description { get; set; }


        // Navigational Properties

        public virtual ICollection<TransitionProfile> TransitionProfiles { get; set; } = [];

        public virtual ICollection<DifficultyLevel> DifficultyLevels { get; set; } = [];

        public virtual ICollection<PaperMetadata> PaperMetadata { get; set; } = [];
    }
}
