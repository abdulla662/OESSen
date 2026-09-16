using OES.Helper.Dtos.NotificationDto;

namespace OES.Blazor.Services.Interfaces.Notification
{
    public interface INotificationManager
    {
        Task InitializeAsync();

        event Action<NotificationDto> OnNotificationReceived;
    }
}
