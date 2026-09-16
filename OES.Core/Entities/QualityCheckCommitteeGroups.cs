using OES.Core.Entities.QuestionQualityCheck;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QualityCheckCommitteeGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long QualityCheckCommitteeId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(QualityCheckCommitteeId))]
        public virtual QualityCheckCommittee QualityCheckCommittee { get; set; }
    }
}
