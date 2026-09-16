using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ILOGroup : BaseEntity<long>
    {
        public long ILOId { get; set; }

        public Guid GroupId { get; set; }

        [ForeignKey(nameof(ILOId))]
        public virtual ILO ILO { get; set; }

        [ForeignKey(nameof(GroupId))]
        public virtual OESGroup OESGroup { get; set; }
    }
}
