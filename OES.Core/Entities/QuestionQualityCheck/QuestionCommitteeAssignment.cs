using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.QuestionQualityCheck
{
    public class QuestionCommitteeAssignment : BaseEntity<long>
    {
        public long QuestionId { get; set; }

        public long CommitteeId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionMetadata Question { get; set; }

        [ForeignKey(nameof(CommitteeId))]
        public virtual QualityCheckCommittee Committee { get; set; }
    }
}
