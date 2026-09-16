using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class AppUserProfileGroup : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public Guid AppUserProfileId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(AppUserProfileId))]
        public virtual AppUserProfile AppUserProfile { get; set; }
    }
}
