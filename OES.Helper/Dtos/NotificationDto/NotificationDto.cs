using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.NotificationDto
{
    public class NotificationDto
    {
        public NotificationEntity Entity { get; set; }

        public NotificationOperation Operation { get; set; }

        public NotificationStatus Status { get; set; }

        public int? AffectedRows { get; set; }

        public string ParameterName { get; set; }

        public string Message => Resource.NotificationMessage(Entity, Operation, Status, ParameterName, AffectedRows);

        public string Subject => Resource.NotificationSubject(Entity, Operation, Status);

        public string From { get; set; }

        public NotificationTypeStatus Type { get; set; } = NotificationTypeStatus.Info;

        public NotificationDto() { }

        public NotificationDto(NotificationEntity entity, NotificationOperation operation, string from, NotificationStatus status = NotificationStatus.Success, NotificationTypeStatus type = NotificationTypeStatus.Info, int? affectedRows = null, string parameterName = null)
        {
            Entity = entity;
            Operation = operation;
            AffectedRows = affectedRows;
            ParameterName = parameterName;
            From = from;
            Status = status;
            Type = type;
        }
    }
}
