namespace OES.Interface.Interfaces
{
    public interface ISyncNotificationService
    {
        Task BroadcastGlobalSyncStartedAsync(long organizationId, DateTime expirationTime, CancellationToken cancellationToken = default);

        Task BroadcastGlobalSyncFinishedAsync(long organizationId, CancellationToken cancellationToken = default);
    }
}
