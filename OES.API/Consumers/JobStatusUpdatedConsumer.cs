using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OES.API.Hubs;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Interface.Interfaces;
using OES.Services.ParallelService;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Enums;
using SharedHelper.General;

namespace OES.API.Consumers
{
    public class JobStatusUpdatedConsumer : IConsumer<JobStatusUpdated>
    {
        private readonly IHubContext<SyncDashboardHub> _hubContext;
        private readonly ParallelQueryService _parallelQueryService;
        private readonly IScheduleService _scheduleService;
        private readonly IMemoryCacheRepository _memoryCacheRepo;
        private readonly ISyncNotificationService _syncNotificationService;

        public JobStatusUpdatedConsumer(
            IHubContext<SyncDashboardHub> hubContext,
            ParallelQueryService parallelQueryService,
            IScheduleService scheduleService,
            IMemoryCacheRepository memoryCacheRepo,
            ISyncNotificationService syncNotificationService
        )
        {
            _hubContext = hubContext;
            _parallelQueryService = parallelQueryService;
            _scheduleService = scheduleService;
            _memoryCacheRepo = memoryCacheRepo;
            _syncNotificationService = syncNotificationService;
        }

        public async Task Consume(ConsumeContext<JobStatusUpdated> context)
        {
            var request = context.Message;

            long? scheduleIdToUpdate = null;

            Guid? currentBatchId = null;

            long currentOrgId = 0;

            await _parallelQueryService.ExecuteWriteAsync(async uow =>
            {
                var repo = uow.Repository<RealTimeSyncJob, long>();

                var job = await repo
                    .Query()
                    .FirstOrDefaultAsync(j => j.JobId == request.JobId);

                if (job != null)
                {
                    currentBatchId = job.BatchId;
                    currentOrgId = job.OrganizationId;
                    job.Status = request.Status;
                    job.ErrorMessage = request.ErrorMessage;
                    job.CompletedAt = request.CompletedAt;

                    if (request.Status == SyncJobStatus.InProgress)
                    {
                        job.StartedProcessingAt = DateTimeHelper.Now;
                    }

                    if (request.Status == SyncJobStatus.Success)
                    {
                        var candidateIds = await uow.Repository<SchedulePaperCandidate, long>()
                            .Query()
                            .Where(spc => spc.SchedulePaper.ScheduleMetadataId == job.ScheduleId && spc.VenueId == job.VenueId)
                            .Select(spc => spc.CandidateId)
                            .Distinct()
                            .ToListAsync();

                        if (candidateIds.Count > 0)
                        {
                            // 1. Update SchedulePaperCandidates rows
                            await uow.Repository<SchedulePaperCandidate, long>()
                                .Query()
                                .Where(spc => spc.SchedulePaper.ScheduleMetadataId == job.ScheduleId && spc.VenueId == job.VenueId)
                                .ExecuteUpdateAsync(setters => setters.SetProperty(spc => spc.IsSynced, true));

                            // 2. Update Candidates rows
                            await uow.Repository<Candidate, long>()
                                .Query()
                                .Where(c => candidateIds.Contains(c.Id))
                                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.IsSynced, true));
                        }

                        scheduleIdToUpdate = job.ScheduleId;
                    }

                    await uow.Complete();

                    await _hubContext.Clients.Group($"schedule-{job.ScheduleId}").SendAsync(
                        "UpdateJobStatus",
                        job.Id,
                        (int)job.Status,
                        job.ErrorMessage,
                        job.CompletedAt
                    );
                }

                return true;
            });

            if (currentBatchId.HasValue && currentOrgId > 0)
            {
                var cacheKey = new OrganizationCacheKey { KeyType = GlobalCacheKeys.GlobalSyncLock, OrganizationId = currentOrgId };

                var lockedData = _memoryCacheRepo.GetItemFromCache<GlobalSyncLockDto, OrganizationCacheKey>(cacheKey);

                if (lockedData != null && lockedData.BatchId == currentBatchId.Value)
                {
                    bool stillRunning = await _parallelQueryService
                            .ExecuteReadAsync<RealTimeSyncJob, long, bool>(async repo =>
                                await repo.Query()
                                          .AnyAsync(j => j.BatchId == lockedData.BatchId &&
                                                        (j.Status == SyncJobStatus.Pending ||
                                                         j.Status == SyncJobStatus.InProgress))
                            );

                    if (!stillRunning)
                    {
                        _memoryCacheRepo.RemoveItemFromCache<OrganizationCacheKey>(cacheKey);

                        await _syncNotificationService.BroadcastGlobalSyncFinishedAsync(currentOrgId);
                    }
                }
            }

            if (scheduleIdToUpdate.HasValue)
            {
                await _scheduleService.UpdateScheduleSyncStatus(scheduleIdToUpdate.Value);
            }
        }
    }
}