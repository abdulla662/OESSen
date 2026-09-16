using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionLayout : BaseEntity<long>
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        [Required, MaxLength(200)]
        public string ComponentName { get; set; }

        public string ImageUrl { get; set; }

        public long QuestionTypeId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; }
    }
}
