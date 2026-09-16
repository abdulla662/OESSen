using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class PageRoles : BaseEntity<long>
    {
        public long PageId { get; set; }

        [ForeignKey(nameof(PageId))]
        public virtual Page Page { get; set; }

        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual OESRole Role { get; set; }
    }
}
