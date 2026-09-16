using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class OESGroupResource : BaseEntity<long>
    {
        public Guid GroupId { get; set; }

        public long ResourceId { get; set; }

        public ResourceType ResourceType { get; set; } = ResourceType.All;


        // Navigational Properties

        [ForeignKey(nameof(GroupId))]
        public OESGroup Group { get; set; }

        [ForeignKey(nameof(ResourceId))]
        public OESResource Resource { get; set; }

        public ICollection<OESGroupResourceRole> ResourceRoles { get; set; } = [];
    }
}
