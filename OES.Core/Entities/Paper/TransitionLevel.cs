using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class TransitionLevel : BaseEntity<long>
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Range(0.0, 1.0)]
        public decimal LowerDScore { get; set; }

        [Range(0.0, 1.0)]
        public decimal UpperDScore { get; set; }

        public long TransitionProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long QuestionCategoryId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(TransitionProfileId))]
        public virtual TransitionProfile TransitionProfile { get; set; }

        [ForeignKey(nameof(QuestionCategoryId))]
        public virtual QuestionCategory QuestionCategory { get; set; }

        [ForeignKey(nameof(DifficultyLevelId))]
        public virtual DifficultyLevel DifficultyLevel { get; set; }
    }
}
