using MassTransit;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.ParallelService;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class JobManagementService : IJobManagementService
    {
        private readonly ICommonService _commonService;
        private readonly ParallelQueryService _parallelQueryService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IPayloadStorageService _PayloadStorageService;

        public JobManagementService(
            ICommonService commonService,
            ParallelQueryService parallelQueryService,
            IPublishEndpoint publishEndpoint,
            IPayloadStorageService payloadStorageService
        )
        {
            _commonService = commonService;
            _parallelQueryService = parallelQueryService;
            _publishEndpoint = publishEndpoint;
            _PayloadStorageService = payloadStorageService;
        }

        public async Task<ApiResponse> RetryFailedJobs(List<long> jobIds, CancellationToken cancellationToken = default)
        {
            var jobs = await _commonService
                ._unitOfWork
                .Repository<RealTimeSyncJob, long>()
                .Query()
                .Where(j => jobIds.Contains(j.Id) && j.Status == SyncJobStatus.Failed)
                .ToListAsync(cancellationToken);

            if (!jobs.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFailedJobs,
                    null
                );
            }

            var venueIdsToRetry = jobs.ConvertAll(j => j.VenueId).Distinct();
            var scheduleIdsToRetry = jobs.ConvertAll(j => j.ScheduleId).Distinct();

            var venueIdToCodeMap = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Where(v => venueIdsToRetry.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Code, cancellationToken);

            var scheduleIdToNameMap = await _commonService
                   ._unitOfWork
                   .Repository<ScheduleMetadata, long>()
                   .Query()
                   .AsNoTracking()
                   .Where(s => scheduleIdsToRetry.Contains(s.Id))
                   .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            var retriedJobs = new List<SyncJobStatusDto>();

            foreach (var failedJob in jobs)
            {
                await _parallelQueryService.ExecuteWriteAsync(async (uow) =>
                {
                    failedJob.Status = SyncJobStatus.Pending;
                    failedJob.StartedProcessingAt = null;
                    failedJob.CompletedAt = null;
                    failedJob.ErrorMessage = null;
                    failedJob.ModeficationDate = DateTimeHelper.Now;

                    uow.Repository<RealTimeSyncJob, long>().Update(failedJob);

                    await uow.Complete();

                    return true;
                });

                var message = new SyncScheduleToVenue
                {
                    JobId = failedJob.JobId,
                    ScheduleId = failedJob.ScheduleId,
                    VenueId = failedJob.VenueId,
                    PayloadFilePath = failedJob.PayloadFilePath,
                    Timestamp = DateTimeHelper.Now
                };

                var venueCode = venueIdToCodeMap.TryGetValue(failedJob.VenueId, out var code) ? code : "Unknown";

                var scheduleName = scheduleIdToNameMap.TryGetValue(failedJob.ScheduleId, out var name) ? name : "Unknown";

                await _publishEndpoint.Publish(message, context => context.SetRoutingKey(venueCode), cancellationToken);

                retriedJobs.Add(new SyncJobStatusDto
                {
                    Id = failedJob.Id,
                    BatchId = failedJob.BatchId,
                    ScheduleId = failedJob.ScheduleId,
                    VenueName = failedJob.VenueName,
                    ScheduleName = scheduleName,
                    Status = SyncJobStatus.Pending,
                    CompletedAt = null,
                    ErrorMessage = null,
                    CandidateCount = failedJob.CandidateCount
                });
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.Accepted,
                Resource.selected,
                retriedJobs
            );
        }

        public async Task<ApiResponse> GetJobsByBatchId(Guid batchId)
        {
            var jobs = await _parallelQueryService.ExecuteReadAsync<RealTimeSyncJob, long, List<RealTimeSyncJob>>(
                async repo => await repo
                    .Query()
                    .AsNoTracking()
                    .Where(j => j.BatchId == batchId)
                    .ToListAsync()
            );

            if (jobs.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    null,
                    null
                );
            }

            var jobDtos = jobs.ConvertAll(job => new SyncJobStatusDto
            {
                Id = job.Id,
                BatchId = job.BatchId,
                ScheduleId = job.ScheduleId,
                VenueName = job.VenueName,
                ScheduleName = job.ScheduleName,
                Status = job.Status,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                PayloadFilePath = job.PayloadFilePath,
                CandidateCount = job.CandidateCount,
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                jobDtos
            );
        }

        public async Task<ApiResponse> GetSyncHistory(DateTime fromDate, CancellationToken cancellationToken = default)
        {
            var jobs = await _parallelQueryService.ExecuteReadAsync<RealTimeSyncJob, long, List<RealTimeSyncJob>>(
                async repo => await repo
                    .Query()
                    .AsNoTracking()
                    .Where(j => j.CreationDate >= fromDate)
                    .OrderByDescending(j => j.CreationDate)
                    .ToListAsync(cancellationToken)
            );

            var jobDtos = jobs.ConvertAll(job => new SyncJobStatusDto
            {
                Id = job.Id,
                BatchId = job.BatchId,
                ScheduleId = job.ScheduleId,
                VenueName = job.VenueName,
                ScheduleName = job.ScheduleName,
                Status = job.Status,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                PayloadFilePath = job.PayloadFilePath,
                CandidateCount = job.CandidateCount,
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                jobDtos
            );
        }

        public async Task<ApiResponse> CancelPendingJob(long jobId, CancellationToken cancellationToken = default)
        {
            var result = await _parallelQueryService.ExecuteWriteAsync(async (uow) =>
            {
                var repo = uow.Repository<RealTimeSyncJob, long>();
                var job = await repo.Query().FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

                if (job == null || job.Status != SyncJobStatus.Pending)
                    return false;

                job.Status = SyncJobStatus.Failed;
                job.ErrorMessage = "Cancelled manually by user";
                job.ModeficationDate = DateTimeHelper.Now;

                repo.Update(job);

                await uow.Complete();
                return true;
            });

            if (!result)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.FailedToSaveData,
                    null
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SavedSuccessfully,
                null
            );
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> GetPayloadStreamAsync(string fileName)
        {
            var stream = await _PayloadStorageService.GetPayloadStreamAsync(fileName);
            var downloadFileName = Path.GetFileName(fileName) ?? "payload.json";
            return (stream, MiscConstants.ApplicationJsonContentType, downloadFileName);
        }
    }
}