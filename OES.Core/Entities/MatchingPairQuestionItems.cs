using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class MatchingPairQuestionItems : BaseEntity<long>
    {
        [Required]
        public string Body { get; set; }

        [Required]
        public int ColumnOrder { get; set; }

        public long QuestionDetailsId { get; set; }

        public bool IsDataSource { get; set; }

        [ForeignKey(nameof(QuestionDetailsId))]
        public virtual QuestionDetails QuestionDetails { get; set; }
    }
}
