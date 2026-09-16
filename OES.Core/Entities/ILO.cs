using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ILO : BaseEntity<long>
    {
        public string Name { get; set; }

        public string ILOSignature { get; set; }

        public long? ParentId { get; set; } = null!;

        [ForeignKey(nameof(ParentId))]
        public virtual ILO ParentILO { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public virtual ICollection<ILOGroup> ILOGroups { get; set; } = [];
    }
}
