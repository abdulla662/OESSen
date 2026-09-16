using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.QuestionQualityCheck
{
    public class QuestionReview : BaseEntity<long>
    {
        [Required]
        public long QuestionId { get; set; }

        [Required]
        public long MemberId { get; set; }

        public bool IsApproved { get; set; }

        public string? Comments { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionMetadata Question { get; set; }

        [ForeignKey(nameof(MemberId))]
        public virtual QualityCheckCommitteeMember QCCommitteeMember { get; set; }
    }
}
