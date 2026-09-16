using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.NotificationDto
{
    public class CreateNewNotificationDto
    {
        public NotificationEntity Entity { get; set; }

        public NotificationOperation Operation { get; set; }

        public int? AffectedRows { get; set; }

        public string? ParameterName { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string From { get; set; }

        public NotificationTypeStatus Type { get; set; } = NotificationTypeStatus.Info;

        public NotificationStatus Status { get; set; } = NotificationStatus.Success;

        public List<Guid> AppUserID { get; set; }

        public string Message => Resource.NotificationMessage(Entity, Operation, Status, ParameterName, AffectedRows);
    }
}