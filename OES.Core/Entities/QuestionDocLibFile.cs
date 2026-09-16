using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionDocLibFile : BaseEntity<long>
    {
        public long QuestionId { get; set; }

        public string FileURL { get; set; }

        [Required]
        public Guid FileId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(QuestionId))]
        public QuestionMetadata QuestionMetadata { get; set; }
    }
}
