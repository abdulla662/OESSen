using Hangfire;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.CBTSyncSetting.Requests;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class CBTSyncSettingService(ICommonService _commonService) : ICBTSyncSettingService
    {
        private const string CbtJobId = "auto-sync-daily-candidates";

        public async Task<ApiResponse> GetPaginatedAsync(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CBTSyncSetting, long>()
                .GetAll()
                .AsNoTracking();

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                         o.CreationDate <= (pagination.ToDate ?? DateTime.Now.Date));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var paginatedData = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                           .Take(pagination.PageSize)
                                           .ToListAsync();

            var responseDtos = paginatedData.ConvertAll(c => new CBTSyncSettingResponseDto(
                c.Id,
                c.CBTAutoSyncEnabled,
                c.CBTSyncScheduleTime
            ));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.DataRetrievedSuccessfully,
                new CustomTableData<CBTSyncSettingResponseDto>(responseDtos, totalItems)
            );
        }

        public async Task<ApiResponse> GetByIdAsync(long id)
        {
            var config = await _commonService
                ._unitOfWork
                .Repository<CBTSyncSetting, long>()
                .GetByIdAsync(id);

            if (config == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.DataNotFound);
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.DataRetrievedSuccessfully,
                new CBTSyncSettingResponseDto(config.Id, config.CBTAutoSyncEnabled, config.CBTSyncScheduleTime)
            );
        }

        public async Task<ApiResponse> UpdateAsync(CBTSyncSettingRequestDto requestDto)
        {
            var config = await _commonService
                ._unitOfWork
                .Repository<CBTSyncSetting, long>()
                .GetByIdAsync(requestDto.Id);

            if (config == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.DataNotFound);

            config.CBTAutoSyncEnabled = requestDto.CBTAutoSyncEnabled;

            config.CBTSyncScheduleTime = requestDto.CBTSyncScheduleTime;

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                UpdateHangfireJob(config.CBTAutoSyncEnabled, config.CBTSyncScheduleTime);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.DataUpdatedSuccessfully,
                    new CBTSyncSettingResponseDto(config.Id, config.CBTAutoSyncEnabled, config.CBTSyncScheduleTime)
                );
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, Resource.FailedToUpdateData);
        }

        private static void UpdateHangfireJob(bool autoSyncEnabled, string syncScheduleTime)
        {
            if (autoSyncEnabled && !string.IsNullOrEmpty(syncScheduleTime))
            {
                var parts = syncScheduleTime.Split(':');

                var hour = int.Parse(parts[0]);

                var minute = parts.Length > 1 ? int.Parse(parts[1]) : 0;

                var cronExpression = $"{minute} {hour} * * *";

                RecurringJob.AddOrUpdate<ICBTCandidatesSyncService>(
                    CbtJobId,
                    service => service.ExecuteSyncAutomaticallyAsync(null),
                    cronExpression,
                    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
                );
            }
            else
            {
                RecurringJob.RemoveIfExists(CbtJobId);
            }
        }
    }
}
