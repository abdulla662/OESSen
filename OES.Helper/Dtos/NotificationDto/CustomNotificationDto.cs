using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.NotificationDto
{
    public class CustomNotificationDto
    {
        public long NotificationId { get; set; }

        public string Subject => Resource.NotificationSubject(Entity, Operation, Status);

        public string From { get; set; }

        public bool IsRead { get; set; }

        public NotificationEntity Entity { get; set; }

        public NotificationOperation Operation { get; set; }

        public NotificationTypeStatus Type { get; set; } = NotificationTypeStatus.Info;

        public NotificationStatus Status { get; set; } = NotificationStatus.Success;

        public int? AffectedRows { get; set; }

        public string ParameterName { get; set; }

        public string Message => Resource.NotificationMessage(Entity, Operation, Status, ParameterName);
    }
}
