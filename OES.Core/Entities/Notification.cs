using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Enums;

namespace OES.Core.Entities
{
    public class Notification : BaseEntity<long>
    {
        public NotificationEntity Entity { get; set; }

        public NotificationOperation Operation { get; set; }

        public string ParameterName { get; set; }

        public int? AffectedRows { get; set; }

        public string From { get; set; }

        public NotificationTypeStatus Type { get; set; } = NotificationTypeStatus.Info;

        public NotificationStatus Status { get; set; }

        public virtual ICollection<NotificationAppUserProfile> AppUserProfileNotifications { get; set; } = [];
    }
}
