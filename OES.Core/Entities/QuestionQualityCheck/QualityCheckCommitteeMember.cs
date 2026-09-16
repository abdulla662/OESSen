using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.QuestionQualityCheck
{
    public class QualityCheckCommitteeMember : BaseEntity<long>
    {
        public long CommitteeId { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey(nameof(CommitteeId))]
        public virtual QualityCheckCommittee Committee { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUserProfile User { get; set; }
    }
}
