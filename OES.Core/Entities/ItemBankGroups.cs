using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ItemBankGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        public long ItemBankId { get; set; }

        [ForeignKey(nameof(ItemBankId))]
        public virtual ItemBank ItemBank { get; set; }
    }
}
