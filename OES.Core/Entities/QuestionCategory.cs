using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class QuestionCategory : BaseEntity<long>
    {
        [Required]
        public string Name { get; set; }

        public string? FinalScoreName { get; set; }

        public decimal? StandardDeviation { get; set; }

        public decimal? StandardError1 { get; set; }

        public decimal? StandardError2 { get; set; }

        public decimal? BaseValue1 { get; set; }

        public decimal? BaseValue2 { get; set; }


        // Navigational Properties

        public virtual ICollection<Block> Blocks { get; set; } = [];

        public virtual ICollection<TransitionLevel> TransitionLevels { get; set; } = [];
    }
}