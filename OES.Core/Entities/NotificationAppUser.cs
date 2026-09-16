using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class NotificationAppUserProfile : BaseEntity<long>
    {
        public Guid AppUserProfileId { get; set; }

        [ForeignKey(nameof(AppUserProfileId))]
        public virtual AppUserProfile AppUserProfile { get; set; }

        public long NotificationId { get; set; }

        [ForeignKey(nameof(NotificationId))]
        public virtual Notification Notification { get; set; }

        public bool IsRead { get; set; }
    }
}
