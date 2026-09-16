using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Services.ParallelService;

namespace OES.API.Hubs
{
    public class SyncDashboardHub : Hub
    {
        private readonly ParallelQueryService _parallelQueryService;
        private readonly IMemoryCacheRepository _memoryCacheRepository;

        public SyncDashboardHub(ParallelQueryService parallelQueryService, IMemoryCacheRepository memoryCacheRepository)
        {
            _parallelQueryService = parallelQueryService;
            _memoryCacheRepository = memoryCacheRepository;
        }

        public async Task JoinScheduleGroup(long scheduleId)
        {
            var groupName = $"schedule-{scheduleId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveScheduleGroup(long scheduleId)
        {
            var groupName = $"schedule-{scheduleId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task RequestCurrentStatus(long scheduleId, Guid batchId)
        {
            var jobs = await _parallelQueryService
                .ExecuteReadAsync<RealTimeSyncJob, long, List<RealTimeSyncJob>>(
                    async repository => await repository
                        .Query()
                        .AsNoTracking()
                        .Where(j => j.ScheduleId == scheduleId && j.BatchId == batchId)
                        .ToListAsync()
                );

            foreach (var job in jobs)
            {
                await Clients.Caller.SendAsync(
                    SignalRCommonConstant.UpdateJobStatus,
                    job.Id,
                    (int)job.Status,
                    job.ErrorMessage,
                    job.CompletedAt
                );
            }
        }

        public async Task JoinOrganizationGroup(long organizationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Org-{organizationId}");
        }

        public GlobalSyncLockDto CheckGlobalSyncState(long organizationId)
        {
            var key = new OrganizationCacheKey { KeyType = GlobalCacheKeys.GlobalSyncLock, OrganizationId = organizationId };

            return _memoryCacheRepository.GetItemFromCache<GlobalSyncLockDto, OrganizationCacheKey>(key);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}