using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class DeltaTypeGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long DeltaTypeId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(DeltaTypeId))]
        public virtual DeltaType DeltaType { get; set; }
    }
}
