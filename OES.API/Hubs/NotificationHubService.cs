using Microsoft.AspNetCore.SignalR;
using OES.API.Hubs;
using OES.Helper.Dtos.NotificationDto;
using OES.Interface.Interfaces;

namespace OES.Api.Hubs
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public async Task NotifyAsync(NotificationDto notification, params Guid[] usersIds)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification), "Notification or AppUserID cannot be null or empty");

            foreach (var userId in usersIds)
            {
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);
            }
        }
    }
}