using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class OESGroupRole : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public Guid OESRoleId { get; set; }


        [ForeignKey(nameof(OESRoleId))]
        public virtual OESRole OESRole { get; set; }


        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }
    }
}
