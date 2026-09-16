using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SharedHelper.General;

namespace OES.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.User is null)
            {
                Context.Abort();
            }

            var userId = Context.User.FindFirst(CustomJwtClaimsTypes.ModuleUserID)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                Context.Abort();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User is null)
            {
                Context.Abort();
            }

            var userId = Context.User.FindFirst(CustomJwtClaimsTypes.ModuleUserID)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                Context.Abort();
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
