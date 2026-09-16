using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.Dtos.Sync;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Collections.Concurrent;
using System.Net;

namespace OES.Services.Services
{
    public class QueueSuspendService : IQueueSuspendService
    {
        private readonly ICommonService _commonService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IExamServerService _examServerService;
        private readonly IBus _bus;
        private readonly ILogger<QueueSuspendService> _logger;

        public QueueSuspendService(
            ICommonService commonService,
            IPublishEndpoint publishEndpoint,
            IExamServerService examServerService,
            IBus bus,
            ILogger<QueueSuspendService> logger)
        {
            _commonService = commonService;
            _publishEndpoint = publishEndpoint;
            _examServerService = examServerService;
            _bus = bus;
            _logger = logger;
        }

        // Paper Methods

        public async Task<ApiResponse> ManagePaperSuspensionsAsync(ManagePaperSuspensionsDto requestDto, CancellationToken cancellationToken = default)
        {
            var newSuspendedVenueCodes = requestDto.SuspendedVenueCodes?.ToHashSet() ?? [];

            var (context, earlyExit) = await FetchPaperContextAsync(requestDto.PaperId, newSuspendedVenueCodes, cancellationToken);

            if (earlyExit != null)
            {
                return earlyExit;
            }

            var (toSuspend, toUnsuspend) = ClassifyVenues(context!);

            var result = await ProcessSuspensionsAsync(
                context!.AllVenues,
                toSuspend,
                toUnsuspend,
                (venue, suspending) => PublishPaperSuspensionAsync(context!.PaperId, venue.VenueCode, suspending, cancellationToken),
                cancellationToken);

            var createdSyncJobs = await ApplyPersistenceAsync(context!, result, cancellationToken);

            return BuildResponse(context!.SkippedSyncingVenues, result, createdSyncJobs);
        }

        public async Task<ApiResponse> GetPaginatedPaperVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            var paperVenueIds = _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .Where(sp => sp.PaperId == paperId)
                .SelectMany(sp => sp.ScheduleMetadata.Venues)
                .Where(sv => sv.Venue != null)
                .Select(sv => sv.VenueId)
                .Distinct();

            var venueQuery = _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Where(v => paperVenueIds.Contains(v.Id))
                .Select(v => new
                {
                    VenueId = v.Id,
                    v.Name,
                    v.IPAddress,
                    v.Code
                })
                .AsNoTracking();

            var exists = await venueQuery.AnyAsync();
            if (!exists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.PaperNotFound,
                    HttpStatusCode.BadRequest,
                    Resource.PaperNotFound);
            }

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKey = paginationSearchModel.SearchKey.Trim().ToLower();
                venueQuery = venueQuery.Where(v =>
                    (v.Name ?? string.Empty).ToLower().Contains(searchKey) ||
                    (v.IPAddress ?? string.Empty).ToLower().Contains(searchKey) ||
                    (v.Code ?? string.Empty).ToLower().Contains(searchKey));
            }

            venueQuery = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? venueQuery.OrderByDescending(v => v.Name).ThenByDescending(v => v.VenueId)
                : venueQuery.OrderBy(v => v.Name).ThenBy(v => v.VenueId);

            var totalItems = await venueQuery.CountAsync();

            var pagedData = paginationSearchModel.PaginationOff
                ? await venueQuery.ToListAsync()
                : await venueQuery
                    .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                    .Take(paginationSearchModel.PageSize)
                    .ToListAsync();

            var suspendedVenueIdSet = await _commonService._unitOfWork
                .Repository<PaperVenueSuspension, long>()
                .Query()
                .Where(p => p.PaperId == paperId && p.Status == AvailabilityStatus.Suspended)
                .Select(p => p.VenueId)
                .ToListAsync();

            var isInitiallyActive = await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .Query()
                .Where(p => p.Id == paperId && p.PaperStatus == AvailabilityStatus.Active)
                .AnyAsync();

            var response = pagedData.ConvertAll(v => new QueueSuspendResponseDto
            {
                PaperId = paperId,
                IsInitiallyActive = isInitiallyActive,
                IsSuspensionAccepted = suspendedVenueIdSet.Contains(v.VenueId),
                ServerName = v.Name ?? Resource.UnknownVenue,
                ServerIp = v.IPAddress ?? Resource.UnknownIP,
                VenueCode = v.Code ?? Resource.Unknown
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperVenuesRetrievedSuccessfully,
                new CustomTableData<QueueSuspendResponseDto>(response, totalItems));
        }

        public async Task<ApiResponse> GetSuspendedVenueCodesByPaperIdAsync(long paperId)
        {
            var suspendedVenueIds = await _commonService._unitOfWork
                .Repository<PaperVenueSuspension, long>()
                .Query()
                .Where(p => p.PaperId == paperId && p.Status == AvailabilityStatus.Suspended)
                .Select(p => p.VenueId)
                .ToListAsync();

            if (suspendedVenueIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Validationcompletedsuccessfully,
                    new List<string>());
            }

            var codes = await _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Where(v => suspendedVenueIds.Contains(v.Id))
                .Select(v => v.Code)
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Validationcompletedsuccessfully,
                codes);
        }


        // Form Services

        public async Task<ApiResponse> ManageFormSuspensionsAsync(ManageFormSuspensionsDto requestDto, CancellationToken cancellationToken = default)
        {
            var newSuspendedVenueCodes = requestDto.SuspendedVenueCodes?.ToHashSet() ?? new HashSet<string>();

            var (context, earlyExit) = await FetchFormContextAsync(requestDto.FormId, newSuspendedVenueCodes, cancellationToken);

            if (earlyExit != null)
            {
                return earlyExit;
            }

            if (context is FormSuspensionContext formCtx)
            {
                newSuspendedVenueCodes.UnionWith(formCtx.PaperLockedVenueCodes);
            }

            var (toSuspend, toUnsuspend) = ClassifyVenues(context!);

            var result = await ProcessSuspensionsAsync(
                context!.AllVenues,
                toSuspend,
                toUnsuspend,
                (venue, suspending) => PublishFormSuspensionAsync(context!.PaperId, venue.VenueCode, requestDto.FormId, suspending, cancellationToken),
                cancellationToken);

            var createdSyncJobs = await ApplyFormPersistenceAsync(context!, result, cancellationToken);

            return BuildResponse(context!.SkippedSyncingVenues, result, createdSyncJobs);
        }

        public async Task<ApiResponse> GetPaginatedFormVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long formId)
        {
            var formEntity = await _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .Query()
                .Where(f => f.Id == formId && !f.IsDeleted)
                .Select(f => new { f.PaperId, f.FormStatus })
                .FirstOrDefaultAsync();

            if (formEntity?.PaperId == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.FormNotFound,
                    HttpStatusCode.BadRequest,
                    Resource.FormNotFound);
            }

            var paperId = formEntity.PaperId.Value;

            var paperVenueIds = _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .Where(sp => sp.PaperId == paperId)
                .SelectMany(sp => sp.ScheduleMetadata.Venues)
                .Where(sv => sv.Venue != null)
                .Select(sv => sv.VenueId)
                .Distinct();

            var venueQuery = _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Where(v => paperVenueIds.Contains(v.Id))
                .Select(v => new
                {
                    VenueId = v.Id,
                    v.Name,
                    v.IPAddress,
                    v.Code
                })
                .AsNoTracking();

            var exists = await venueQuery.AnyAsync();
            if (!exists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.PaperNotFound,
                    HttpStatusCode.BadRequest,
                    Resource.PaperNotFound);
            }

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKey = paginationSearchModel.SearchKey.Trim().ToLower();
                venueQuery = venueQuery.Where(v =>
                    (v.Name ?? string.Empty).ToLower().Contains(searchKey) ||
                    (v.IPAddress ?? string.Empty).ToLower().Contains(searchKey) ||
                    (v.Code ?? string.Empty).ToLower().Contains(searchKey));
            }

            venueQuery = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? venueQuery.OrderByDescending(v => v.Name).ThenByDescending(v => v.VenueId)
                : venueQuery.OrderBy(v => v.Name).ThenBy(v => v.VenueId);

            var totalItems = await venueQuery.CountAsync();

            var pagedData = paginationSearchModel.PaginationOff
                ? await venueQuery.ToListAsync()
                : await venueQuery
                    .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                    .Take(paginationSearchModel.PageSize)
                    .ToListAsync();

            var suspendedVenueIdSet = await _commonService._unitOfWork
                .Repository<FormVenueSuspension, long>()
                .Query()
                .Where(fvs => fvs.FormId == formId && fvs.Status == AvailabilityStatus.Suspended)
                .Select(fvs => fvs.VenueId)
                .ToListAsync();

            var paperLockedVenueIdSet = await _commonService._unitOfWork
                .Repository<PaperVenueSuspension, long>()
                .Query()
                .Where(pvs => pvs.PaperId == paperId && pvs.Status == AvailabilityStatus.Suspended)
                .Select(pvs => pvs.VenueId)
                .ToListAsync();

            var paperLockedSet = paperLockedVenueIdSet.ToHashSet();

            bool isInitiallyActive = formEntity.FormStatus == AvailabilityStatus.Active;

            var response = pagedData.ConvertAll(v => new QueueSuspendResponseDto
            {
                PaperId = paperId,
                IsInitiallyActive = isInitiallyActive,
                IsSuspensionAccepted = suspendedVenueIdSet.Contains(v.VenueId),
                IsPaperLocked = paperLockedSet.Contains(v.VenueId),
                ServerName = v.Name ?? Resource.UnknownVenue,
                ServerIp = v.IPAddress ?? Resource.UnknownIP,
                VenueCode = v.Code ?? Resource.Unknown
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperVenuesRetrievedSuccessfully,
                new CustomTableData<QueueSuspendResponseDto>(response, totalItems));
        }

        public async Task<ApiResponse> GetSuspendedVenueCodesByFormIdAsync(long formId)
        {
            var suspendedVenueIds = await _commonService._unitOfWork
                .Repository<FormVenueSuspension, long>()
                .Query()
                .Where(fvs => fvs.FormId == formId && fvs.Status == AvailabilityStatus.Suspended)
                .Select(fvs => fvs.VenueId)
                .ToListAsync();

            if (suspendedVenueIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Validationcompletedsuccessfully,
                    new List<string>());
            }

            var codes = await _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Where(v => suspendedVenueIds.Contains(v.Id))
                .Select(v => v.Code)
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Validationcompletedsuccessfully,
                codes);
        }

        #region Shared Fetching Helpers
        private async Task<(SharedFetchResult? Result, ApiResponse? EarlyExit)> FetchSharedVenueDataAsync(long paperId, CancellationToken cancellationToken)
        {
            var schedulePapersList = await _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .Where(sp => sp.PaperId == paperId)
                .Select(sp => new SchedulePaperEntry(
                    sp.PaperMetadata,
                    sp.ScheduleMetadataId,
                    sp.ScheduleMetadata.Venues
                        .Where(sv => sv.Venue != null)
                        .Select(sv => new VenueInfo(
                            sv.VenueId,
                            sv.Venue.Code ?? Resource.Unknown,
                            sv.Venue.Name ?? Resource.UnknownVenue))
                        .ToList()))
                .ToListAsync(cancellationToken);

            if (schedulePapersList.Count == 0 || schedulePapersList[0].Paper == null)
            {
                return (null, _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.PaperNotFound,
                    HttpStatusCode.BadRequest,
                    Resource.PaperNotFound));
            }

            var allVenues = schedulePapersList
                .SelectMany(r => r.Venues)
                .GroupBy(v => v.VenueId)
                .Select(g => g.First())
                .ToList();

            var allVenueIds = allVenues.Select(v => v.VenueId).ToList();
            var scheduleIds = schedulePapersList.Select(s => s.ScheduleId).ToList();

            var syncingJobs = await _commonService._unitOfWork
                .Repository<RealTimeSyncJob, long>()
                .Query()
                .Where(j => scheduleIds.Contains(j.ScheduleId) && allVenueIds.Contains(j.VenueId) && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.InProgress))
                .Select(j => new { j.VenueId, j.VenueName })
                .Distinct()
                .ToListAsync(cancellationToken);

            return (new SharedFetchResult(
                AllVenues: allVenues,
                VenueById: allVenues.ToDictionary(v => v.VenueId),
                ScheduleIds: scheduleIds,
                SchedulePapersList: schedulePapersList,
                SyncingVenueIdSet: [.. syncingJobs.Select(j => j.VenueId)],
                SkippedSyncingVenues: syncingJobs
                    .Select(j => j.VenueName)
                    .Where(n => n != null)
                    .ToList()!), null);
        }

        private async Task<(SuspensionContext? Context, ApiResponse? EarlyExit)> FetchPaperContextAsync(long paperId, HashSet<string> newSuspendedVenueCodes, CancellationToken cancellationToken)
        {
            var (shared, earlyExit) = await FetchSharedVenueDataAsync(paperId, cancellationToken);

            if (earlyExit is not null)
            {
                return (null, earlyExit);
            }

            var allExistingSuspensions = await _commonService._unitOfWork
                .Repository<PaperVenueSuspension, long>()
                .Query()
                .Where(p => p.PaperId == paperId)
                .ToListAsync(cancellationToken);

            var existingSuspendedCodeSet = allExistingSuspensions
                .Where(s => s.Status == AvailabilityStatus.Suspended && shared!.VenueById.ContainsKey(s.VenueId))
                .Select(s => shared!.VenueById[s.VenueId].VenueCode)
                .ToHashSet();

            var context = new SuspensionContext
            {
                PaperId = paperId,
                NewSuspendedVenueCodes = newSuspendedVenueCodes,
                Paper = shared!.SchedulePapersList[0].Paper,
                IsInitiallyActive = shared!.SchedulePapersList[0].Paper.PaperStatus == AvailabilityStatus.Active,
                AllVenues = shared.AllVenues,
                VenueById = shared.VenueById,
                SuspensionByVenueId = allExistingSuspensions.ToDictionary(s => s.VenueId),
                ExistingSuspendedVenueIds = [.. allExistingSuspensions
                    .Where(s => s.Status == AvailabilityStatus.Suspended)
                    .Select(s => s.VenueId)],
                ExistingSuspendedCodeSet = existingSuspendedCodeSet,
                ScheduleIds = shared.ScheduleIds,
                SchedulePapersList = shared.SchedulePapersList,
                SyncingVenueIdSet = shared.SyncingVenueIdSet,
                SkippedSyncingVenues = shared.SkippedSyncingVenues
            };

            return (context, null);
        }

        private async Task<(FormSuspensionContext? Context, ApiResponse? EarlyExit)> FetchFormContextAsync(long formId, HashSet<string> newSuspendedVenueCodes, CancellationToken cancellationToken)
        {
            var formEntity = await _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .Query()
                .Where(f => f.Id == formId && !f.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (formEntity?.PaperId == null)
            {
                return (null, _commonService._apiResponse.GetApiResponse(CustomCodeStatus.FormNotFound,
                                                                         HttpStatusCode.BadRequest,
                                                                         Resource.FormNotFound));
            }

            var paperId = formEntity.PaperId.Value;

            var (shared, earlyExit) = await FetchSharedVenueDataAsync(paperId, cancellationToken);

            if (earlyExit is not null)
            {
                return (null, earlyExit);
            }

            var allExistingSuspensions = await _commonService._unitOfWork
                .Repository<FormVenueSuspension, long>()
                .Query()
                .Where(p => p.FormId == formId)
                .ToListAsync(cancellationToken);

            var existingSuspendedCodeSet = allExistingSuspensions
                .Where(s => s.Status == AvailabilityStatus.Suspended && shared!.VenueById.ContainsKey(s.VenueId))
                .Select(s => shared!.VenueById[s.VenueId].VenueCode)
                .ToHashSet();

            var paperLockedVenueIds = await _commonService._unitOfWork
                .Repository<PaperVenueSuspension, long>()
                .Query()
                .Where(p => p.PaperId == paperId && p.Status == AvailabilityStatus.Suspended)
                .Select(p => p.VenueId)
                .ToListAsync(cancellationToken);

            var paperLockedCodeSet = shared!.AllVenues
                .Where(v => paperLockedVenueIds.Contains(v.VenueId))
                .Select(v => v.VenueCode)
                .ToHashSet();

            var context = new FormSuspensionContext
            {
                PaperId = paperId,
                Form = formEntity,
                IsInitiallyActive = formEntity.FormStatus == AvailabilityStatus.Active,
                NewSuspendedVenueCodes = newSuspendedVenueCodes,
                AllVenues = shared!.AllVenues,
                VenueById = shared.VenueById,
                SuspensionByVenueId = allExistingSuspensions.ToDictionary(s => s.VenueId),
                ExistingSuspendedVenueIds = [.. allExistingSuspensions
                    .Where(s => s.Status == AvailabilityStatus.Suspended)
                    .Select(s => s.VenueId)],
                ExistingSuspendedCodeSet = existingSuspendedCodeSet,
                PaperLockedVenueCodes = paperLockedCodeSet,
                ScheduleIds = shared.ScheduleIds,
                SchedulePapersList = shared.SchedulePapersList,
                SyncingVenueIdSet = shared.SyncingVenueIdSet,
                SkippedSyncingVenues = shared.SkippedSyncingVenues
            };

            return (context, null);
        }
        #endregion

        #region Shared Pipeline Steps Helpers
        private static (List<VenueInfo> ToSuspend, List<VenueInfo> ToUnsuspend) ClassifyVenues(SuspensionContextBase ctx)
        {
            var toSuspend = ctx.AllVenues
                .Where(v => ctx.NewSuspendedVenueCodes.Contains(v.VenueCode) && !ctx.ExistingSuspendedCodeSet.Contains(v.VenueCode) && !ctx.SyncingVenueIdSet.Contains(v.VenueId))
                .ToList();

            var toUnsuspend = ctx.AllVenues
                .Where(v => !ctx.NewSuspendedVenueCodes.Contains(v.VenueCode) && (ctx.IsInitiallyActive || ctx.ExistingSuspendedCodeSet.Contains(v.VenueCode)) && !ctx.SyncingVenueIdSet.Contains(v.VenueId))
                .ToList();

            return (toSuspend, toUnsuspend);
        }

        private async Task<SuspensionProcessResult> ProcessSuspensionsAsync(
            List<VenueInfo> allVenues,
            List<VenueInfo> toSuspend,
            List<VenueInfo> toUnsuspend,
            Func<VenueInfo, bool, Task> publishAction,
            CancellationToken cancellationToken)
        {
            var failedNames = new ConcurrentBag<string>();
            var suspendedIds = new ConcurrentBag<long>();
            var unsuspendedIds = new ConcurrentBag<long>();

            async Task ProcessOne(VenueInfo venue, bool suspending)
            {
                try
                {
                    if (!await PingVenueAsync(venue.VenueCode, cancellationToken))
                    {
                        failedNames.Add(venue.Name);
                        return;
                    }

                    await publishAction(venue, suspending);

                    if (suspending)
                    {
                        suspendedIds.Add(venue.VenueId);
                    }
                    else
                    {
                        unsuspendedIds.Add(venue.VenueId);
                    }
                }
                catch (Exception)
                {
                    failedNames.Add(venue.Name);
                }
            }

            var tasks = toSuspend
                .Select(v => ProcessOne(v, true))
                .Concat(toUnsuspend.Select(v => ProcessOne(v, false)));

            await Task.WhenAll(tasks);

            return new SuspensionProcessResult(
                [.. suspendedIds],
                [.. unsuspendedIds],
                [.. failedNames]);
        }
        #endregion

        #region Paper & Form Persistence Helpers
        private async Task<List<SyncJobStatusDto>> ApplyPersistenceAsync(
            SuspensionContext ctx,
            SuspensionProcessResult result,
            CancellationToken cancellationToken)
        {
            foreach (var venueId in result.SuspendedVenueIds)
            {
                if (ctx.SuspensionByVenueId.TryGetValue(venueId, out var existing))
                {
                    existing.Status = AvailabilityStatus.Suspended;
                }
                else
                {
                    await _commonService
                        ._unitOfWork
                        .Repository<PaperVenueSuspension, long>()
                        .AddAsync(new PaperVenueSuspension
                        {
                            PaperId = ctx.PaperId,
                            VenueId = venueId,
                            Status = AvailabilityStatus.Suspended
                        });
                }
            }

            foreach (var venueId in result.UnsuspendedVenueIds)
            {
                if (ctx.SuspensionByVenueId.TryGetValue(venueId, out var existing))
                {
                    existing.Status = AvailabilityStatus.Synced;
                }
            }

            var finalSuspendedIdSet = ctx.ExistingSuspendedVenueIds
                .Except(result.UnsuspendedVenueIds)
                .Union(result.SuspendedVenueIds)
                .ToHashSet();

            ctx.Paper.PaperStatus = (finalSuspendedIdSet.Count >= ctx.AllVenues.Count && ctx.AllVenues.Count > 0)
                ? AvailabilityStatus.Suspended
                : AvailabilityStatus.Synced;

            var createdSyncJobs = new List<SyncJobStatusDto>();

            if (result.SuspendedVenueIds.Count > 0 || result.UnsuspendedVenueIds.Count > 0)
            {
                var paperForms = await _commonService._unitOfWork
                    .Repository<PaperForm, long>()
                    .Query()
                    .Where(pf => pf.PaperId == ctx.PaperId && !pf.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var form in paperForms)
                    form.FormStatus = ctx.Paper.PaperStatus;

                // Cascade FormVenueSuspension records for each form
                var formIds = paperForms.ConvertAll(f => f.Id);
                var existingFormSuspensions = await _commonService._unitOfWork
                    .Repository<FormVenueSuspension, long>()
                    .Query()
                    .Where(fvs => formIds.Contains(fvs.FormId))
                    .ToListAsync(cancellationToken);

                var formSuspensionLookup = existingFormSuspensions
                    .ToLookup(fvs => (fvs.FormId, fvs.VenueId));

                foreach (var form in paperForms)
                {
                    foreach (var venueId in result.SuspendedVenueIds)
                    {
                        var existing = formSuspensionLookup[(form.Id, venueId)].FirstOrDefault();
                        if (existing != null)
                        {
                            existing.Status = AvailabilityStatus.Suspended;
                        }
                        else
                        {
                            await _commonService._unitOfWork
                                .Repository<FormVenueSuspension, long>()
                                .AddAsync(new FormVenueSuspension
                                {
                                    FormId = form.Id,
                                    VenueId = venueId,
                                    Status = AvailabilityStatus.Suspended
                                });
                        }
                    }

                    foreach (var venueId in result.UnsuspendedVenueIds)
                    {
                        var existing = formSuspensionLookup[(form.Id, venueId)].FirstOrDefault();
                        if (existing != null)
                        {
                            existing.Status = AvailabilityStatus.Synced;
                        }
                    }
                }

                if (result.UnsuspendedVenueIds.Count > 0)
                {
                    createdSyncJobs.AddRange(await TriggerBulkSyncAsync(ctx.SchedulePapersList, ctx.PaperId, formId: null, result.UnsuspendedVenueIds, cancellationToken));
                }
            }

            await _commonService._unitOfWork.Complete();

            return createdSyncJobs;
        }

        private async Task<List<SyncJobStatusDto>> ApplyFormPersistenceAsync(
            FormSuspensionContext ctx,
            SuspensionProcessResult result,
            CancellationToken cancellationToken)
        {
            foreach (var venueId in result.SuspendedVenueIds)
            {
                if (ctx.SuspensionByVenueId.TryGetValue(venueId, out var existing))
                {
                    existing.Status = AvailabilityStatus.Suspended;
                }
                else
                {
                    await _commonService._unitOfWork
                        .Repository<FormVenueSuspension, long>()
                        .AddAsync(new FormVenueSuspension
                        {
                            FormId = ctx.Form.Id,
                            VenueId = venueId,
                            Status = AvailabilityStatus.Suspended
                        });
                }
            }

            foreach (var venueId in result.UnsuspendedVenueIds)
            {
                if (ctx.SuspensionByVenueId.TryGetValue(venueId, out var existing))
                {
                    existing.Status = AvailabilityStatus.Synced;
                }
            }

            var finalSuspendedIdSet = ctx.ExistingSuspendedVenueIds
                .Except(result.UnsuspendedVenueIds)
                .Union(result.SuspendedVenueIds)
                .ToHashSet();

            ctx.Form.FormStatus = (finalSuspendedIdSet.Count >= ctx.AllVenues.Count && ctx.AllVenues.Count > 0)
                ? AvailabilityStatus.Suspended
                : AvailabilityStatus.Synced;

            var createdSyncJobs = new List<SyncJobStatusDto>();

            if (result.UnsuspendedVenueIds.Count > 0)
            {
                createdSyncJobs.AddRange(await TriggerBulkSyncAsync(ctx.SchedulePapersList, ctx.PaperId, formId: ctx.Form.Id, result.UnsuspendedVenueIds, cancellationToken));
            }

            await _commonService._unitOfWork.Complete();
            return createdSyncJobs;
        }
        #endregion

        #region Shared Infra Helpers
        private async Task<List<SyncJobStatusDto>> TriggerBulkSyncAsync(
            List<SchedulePaperEntry> schedulePapersList,
            long paperId,
            long? formId,
            HashSet<long> unsuspendedVenueIds,
            CancellationToken cancellationToken)
        {
            var syncTasks = schedulePapersList.Select(s =>
                _examServerService.BulkSync(
                    s.ScheduleId,
                    paperId,
                    formId,
                    isPartialSync: true,
                    venueIds: [.. s.Venues
                        .Where(v => unsuspendedVenueIds.Contains(v.VenueId))
                        .Select(v => v.VenueId)],
                    cancellationToken: cancellationToken));

            var responses = await Task.WhenAll(syncTasks);

            return [.. responses
                .Where(r => r?.Data != null)
                .SelectMany(r => r.Data is IEnumerable<SyncJobStatusDto> jobs
                    ? jobs
                    : [])];
        }

        private async Task PublishPaperSuspensionAsync(long paperId, string venueCode, bool isSuspending, CancellationToken cancellationToken)
        {
            await _publishEndpoint.Publish(
                new SuspendPaperToVenue
                {
                    PaperId = paperId,
                    IsSuspensionAccepted = isSuspending,
                    Timestamp = DateTimeHelper.Now
                },
                ctx => ctx.SetRoutingKey(venueCode),
                cancellationToken);
        }

        private async Task PublishFormSuspensionAsync(long paperId, string venueCode, long formId, bool isSuspending, CancellationToken cancellationToken)
        {
            await _publishEndpoint.Publish(
                new SuspendFormToVenue
                {
                    PaperId = paperId,
                    FormId = formId,
                    IsSuspensionAccepted = isSuspending,
                    Timestamp = DateTimeHelper.Now
                },
                ctx => ctx.SetRoutingKey(venueCode),
                cancellationToken);
        }

        private async Task<bool> PingVenueAsync(string venueCode, CancellationToken cancellationToken)
        {
            try
            {
                var destination = new Uri($"exchange:{SharedHelper.General.SharedConstants.ScheduleExchangeName}?type=topic");
                var client = _bus.CreateRequestClient<IPingVenueConnection>(destination);

                await client.GetResponse<IPongVenueConnection>(
                    new { VenueCode = venueCode },
                    callback: x => x.UseExecute(ctx => ctx.SetRoutingKey(venueCode)),
                    cancellationToken: cancellationToken,
                    timeout: RequestTimeout.After(s: 2));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private ApiResponse BuildResponse(
            List<string> skippedSyncingVenues,
            SuspensionProcessResult result,
            List<SyncJobStatusDto> createdSyncJobs)
        {
            var parts = new List<string>();

            if (skippedSyncingVenues.Count > 0)
                parts.Add(Resource.SkippedSyncingVenues + " " + string.Join(", ", skippedSyncingVenues));

            if (result.FailedVenueNames.Count > 0)
                parts.Add(Resource.UnreachableVenues + " " + string.Join(", ", result.FailedVenueNames));

            var message = parts.Count > 0
                ? Resource.CompletedWithWarnings + string.Join(". ", parts)
                : Resource.Completed;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                parts.Count > 0 ? HttpStatusCode.MultiStatus : HttpStatusCode.OK,
                message,
                createdSyncJobs.Count > 0 ? createdSyncJobs : null);
        }
        #endregion

        #region DTOs
        private record FormSuspensionContext : SuspensionContextBase
        {
            public required long PaperId { get; init; }
            public required PaperForm Form { get; init; }
            public required Dictionary<long, VenueInfo> VenueById { get; init; }
            public required Dictionary<long, FormVenueSuspension> SuspensionByVenueId { get; init; }
            public required HashSet<long> ExistingSuspendedVenueIds { get; init; }
            public required HashSet<string> PaperLockedVenueCodes { get; init; }
            public required List<long> ScheduleIds { get; init; }
            public required List<SchedulePaperEntry> SchedulePapersList { get; init; }
            public required List<string> SkippedSyncingVenues { get; init; }
        }

        private record SchedulePaperEntry(
                      PaperMetadata Paper,
                      long ScheduleId,
                      List<VenueInfo> Venues);

        private record SharedFetchResult(
                      List<VenueInfo> AllVenues,
                      Dictionary<long, VenueInfo> VenueById,
                      List<long> ScheduleIds,
                      List<SchedulePaperEntry> SchedulePapersList,
                      HashSet<long> SyncingVenueIdSet,
                      List<string> SkippedSyncingVenues);

        private record SuspensionContext : SuspensionContextBase
        {
            public required long PaperId { get; init; }
            public required PaperMetadata Paper { get; set; }
            public required Dictionary<long, VenueInfo> VenueById { get; init; }
            public required Dictionary<long, PaperVenueSuspension> SuspensionByVenueId { get; init; }
            public required HashSet<long> ExistingSuspendedVenueIds { get; init; }
            public required List<long> ScheduleIds { get; init; }
            public required List<SchedulePaperEntry> SchedulePapersList { get; init; }
            public required List<string> SkippedSyncingVenues { get; init; }
        }
        #endregion
    }
}