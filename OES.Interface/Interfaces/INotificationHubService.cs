using OES.Helper.Dtos.NotificationDto;

namespace OES.Interface.Interfaces;

public interface INotificationHubService
{
    Task NotifyAsync(NotificationDto notification, params Guid[] usersIds);
}
