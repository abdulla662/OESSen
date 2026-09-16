using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class OESGroupResourceRole : BaseEntity<long>
    {
        public long GroupResourceId { get; set; }

        public Guid RoleId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(GroupResourceId))]
        public OESGroupResource GroupResource { get; set; }

        [ForeignKey(nameof(RoleId))]
        public OESRole Role { get; set; }
    }
}
