using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities.QuestionQualityCheck
{
    public class QualityCheckCommittee : BaseEntity<long>
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        public Guid? ChiefId { get; set; }

        public virtual AppUserProfile Chief { get; set; }

        public virtual ICollection<QualityCheckCommitteeMember> Members { get; set; } = [];

        public virtual ICollection<QualityCheckCommitteeItemBank> QualityCheckCommitteeItemBanks { get; set; } = [];
    }
}
