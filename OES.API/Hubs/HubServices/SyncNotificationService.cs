using Microsoft.AspNetCore.SignalR;
using OES.Interface.Interfaces;

namespace OES.API.Hubs.HubServices
{
    public class SyncNotificationService : ISyncNotificationService
    {
        private readonly IHubContext<SyncDashboardHub> _hubContext;

        public SyncNotificationService(IHubContext<SyncDashboardHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastGlobalSyncStartedAsync(long organizationId, DateTime expirationTime, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients
                .Group($"Org-{organizationId}")
                .SendAsync("GlobalSyncStarted", organizationId, expirationTime, cancellationToken: cancellationToken);
        }

        public async Task BroadcastGlobalSyncFinishedAsync(long organizationId, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients
                .Group($"Org-{organizationId}")
                .SendAsync("GlobalSyncFinished", organizationId, cancellationToken: cancellationToken);
        }
    }
}
