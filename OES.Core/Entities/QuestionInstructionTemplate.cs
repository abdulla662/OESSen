using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class QuestionInstructionTemplate : BaseEntity<long>
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
