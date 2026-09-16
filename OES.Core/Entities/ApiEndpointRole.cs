using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ApiEndpointRole : BaseEntity<long>
    {
        public long ApiId { get; set; }

        [ForeignKey(nameof(ApiId))]
        public virtual ApiEndpoint ApiEndpoint { get; set; }

        public Guid RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual OESRole Role { get; set; }
    }
}
