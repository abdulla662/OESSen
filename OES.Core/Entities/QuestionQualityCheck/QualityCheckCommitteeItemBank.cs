using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.QuestionQualityCheck
{
    public class QualityCheckCommitteeItemBank : BaseEntity<long>
    {
        public long QualityCheckCommitteeId { get; set; }

        public long ItemBankId { get; set; }

        [ForeignKey(nameof(QualityCheckCommitteeId))]
        public virtual QualityCheckCommittee Committee { get; set; }

        [ForeignKey(nameof(ItemBankId))]
        public virtual ItemBank ItemBank { get; set; }
    }
}
