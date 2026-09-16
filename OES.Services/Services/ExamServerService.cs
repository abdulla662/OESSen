using AngleSharp.Dom;
using MassTransit;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos;
using OES.Helper.Dtos.ExamServer;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Section;
using OES.Helper.Dtos.Sync;
using OES.Helper.Dtos.SyncQuestions;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Interface.Interfaces;
using OES.Services.Helpers;
using OES.Services.ParallelService;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services;

public class ExamServerService : IExamServerService
{
    private readonly ICommonService _commonService;
    private readonly FilterParamsValues _filterParamsValues;
    private readonly ParallelQueryService _parallelQueryService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPayloadStorageService _payloadStorageService;
    private readonly IBus _bus;
    private readonly IMemoryCacheRepository _memoryCacheRepository;
    private readonly ISyncNotificationService _syncNotificationService;

    public ExamServerService(
        ICommonService commonService,
        FilterParamsValues filterParamsValues,
        ParallelQueryService parallelQueryService,
        IPublishEndpoint publishEndpoint,
        IPayloadStorageService payloadStorageService,
        IBus bus,
        IMemoryCacheRepository memoryCacheRepository,
        ISyncNotificationService syncNotificationService
    )
    {
        _commonService = commonService;
        _filterParamsValues = filterParamsValues;
        _parallelQueryService = parallelQueryService;
        _publishEndpoint = publishEndpoint;
        _payloadStorageService = payloadStorageService;
        _bus = bus;
        _memoryCacheRepository = memoryCacheRepository;
        _syncNotificationService = syncNotificationService;
    }

    public async Task<ApiResponse> ValidateScheduleForSyncAsync(long scheduleId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTimeHelper.Now);

        var scheduleValidation = await _commonService
            ._unitOfWork
            .Repository<ScheduleMetadata, long>()
            .Query()
            .Where(s => s.Id == scheduleId && !s.IsDeleted)
            .Select(s => new
            {
                ScheduleExists = true,
                IsExpired = s.EndDate < today,

                ActivePapers = s.Papers
                    .Where(sp =>
                        !sp.IsDeleted &&
                        sp.IsActive &&
                        sp.EndDate >= today)
                    .Select(sp => new
                    {
                        sp.PaperId,
                        FormsCount = sp.PaperMetadata.Forms.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduleValidation == null)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.NotFound,
                Resource.ScheduleNotFound,
                null
            );
        }

        if (scheduleValidation.IsExpired)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                Resource.ScheduleExpired,
                null
            );
        }

        if (scheduleValidation.ActivePapers.Count == 0)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                Resource.ScheduleHasNoPapers,
                null
            );
        }

        var papersWithoutForms = scheduleValidation.ActivePapers
            .Where(p => p.FormsCount == 0)
            .Select(p => p.PaperId)
            .ToList();

        if (papersWithoutForms.Count > 0)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                Resource.NoActivePaperFormsFound,
                papersWithoutForms
            );
        }

        return _commonService._apiResponse.GetApiResponse(
            CustomCodeStatus.Success,
            HttpStatusCode.OK,
            Resource.Success,
            null
        );
    }

    public async Task<ApiResponse> BulkSync(
        long scheduleId,
        long? paperId = null,
        long? formId = null,
        bool isPartialSync = false,
         bool isAutoSync = false,
        List<long> venueIds = null,
        List<long> candidateIdsParam = null,
        AuditContextDto audit = null,
        CancellationToken cancellationToken = default
    )
    {
        audit ??= AuditContextDto.FromHttpContext(_filterParamsValues);

        var batchId = Guid.NewGuid();

        if (!isAutoSync)
        {
            var lockResponse = await LockSync(batchId, cancellationToken);

            if (lockResponse.StatusCode != HttpStatusCode.OK)
            {
                return lockResponse;
            }
        }

        var allSyncJobs = await GetSyncJobsAsync(scheduleId, venueIds, cancellationToken);

        if (!allSyncJobs.Any())
        {
            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "No valid schedules or venues found to sync.", null);
        }

        var scheduleIdsToSync = allSyncJobs.Select(j => j.ScheduleId).Distinct().ToList();

        List<long> venueIdsToSync;

        if (venueIds == null || venueIds.Count == 0)
        {
            venueIdsToSync = [.. allSyncJobs.Select(j => j.VenueId).Distinct()];
        }
        else
        {
            venueIdsToSync = [.. allSyncJobs.Where(j => venueIds.Contains(j.VenueId)).Select(x => x.VenueId).Distinct()];
        }

        //var venueUrlsTask = GetVenueUrlsAsync(venueIdsToSync, cancellationToken);

        var scheduleDataTask = FetchScheduleDataAsync(scheduleIdsToSync, cancellationToken);

        var paperDataTask = FetchPaperDataAsync(scheduleId, paperId, cancellationToken);

        var candidateDataTask = FetchCandidateDataAsync(scheduleIdsToSync, venueIdsToSync, candidateIdsParam, cancellationToken);

        var disabilityDataTask = FetchDisabilityDataAsync(cancellationToken);

        await Task.WhenAll(scheduleDataTask, paperDataTask, candidateDataTask, disabilityDataTask);

        //var venueUrls = await venueUrlsTask;

        var scheduleDtoLookup = await scheduleDataTask;

        var (paperDtoLookup, paperIds, schedulePapersIds) = await paperDataTask;

        var (userDtos, candidateDetailsDtos, candidatePaperDtos, candidateLookup, schedulePaperCandidateIds, syncedCandidateIds) = await candidateDataTask;

        var disabilityDtoLookup = await disabilityDataTask;

        // Dependent fetches: These need IDs from the previous results.
        var (formDtoLookup, formIds) = await FetchFormDataAsync(paperIds, schedulePapersIds, formId, cancellationToken);

        var formToPaperMap = formDtoLookup
            .SelectMany(g => g.Select(f => new { FormId = f.OriginalPaperFormId, PaperId = g.Key }))
            .ToDictionary(x => x.FormId, x => x.PaperId);

        var sectionDtoLookup = await FetchSectionDataAsync(paperIds, formIds, formToPaperMap, cancellationToken);

        bool hasAdaptivePapers = paperDtoLookup.SelectMany(p => p).Any(p => p.Type == nameof(PaperType.Adaptive));

        ILookup<long, StageSyncResponseDto> stageDtoLookup;
        ILookup<long, BlockSyncResponseDto> blockDtoLookup;

        if (hasAdaptivePapers)
        {
            stageDtoLookup = await FetchStageDataAsync(formIds, cancellationToken);

            blockDtoLookup = await FetchBlockDataAsync(formIds, cancellationToken);
        }
        else
        {
            stageDtoLookup = Enumerable.Empty<StageSyncResponseDto>().ToLookup(_ => 0L);

            blockDtoLookup = Enumerable.Empty<BlockSyncResponseDto>().ToLookup(_ => 0L);
        }

        var (questionDtoLookup, questionValidationError) = await FetchQuestionDataAsync(formIds, cancellationToken);

        if (questionValidationError != null)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                questionValidationError,
                null
            );
        }

        var venueDetailsMap = await _commonService
            ._unitOfWork
            .Repository<Venue, long>()
            .Query()
            .AsNoTracking()
            .Where(v => venueIdsToSync.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => (v.Name, v.Code), cancellationToken);

        var syncTasks = allSyncJobs.Select(job =>
            ProcessSyncJobAsync(
                job,
                batchId,
                venueDetailsMap,
                scheduleDtoLookup,
                paperDtoLookup,
                formDtoLookup,
                sectionDtoLookup,
                stageDtoLookup,
                blockDtoLookup,
                questionDtoLookup,
                disabilityDtoLookup,
                candidateLookup,
                userDtos,
                candidateDetailsDtos,
                candidatePaperDtos,
                isPartialSync,
                audit,
                cancellationToken
            )
        );

        var results = await Task.WhenAll(syncTasks);

        var failures = results.Where(r => !r.IsSuccess).ToList();

        var successfulJobs = results.Where(r => r.IsSuccess).Select(r => r.CreatedJob).ToList();

        if (failures.Count > 0)
        {
            if (!isAutoSync)
            {
                await ReleaseGlobalLockAsync(cancellationToken);
            }

            var errorSummary = string.Join(" | ", failures.Select(f => f.ErrorMessage).Distinct());

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                errorSummary,
                null
            );
        }

        var jobDtos = successfulJobs.ConvertAll(job =>
        {
            var scheduleName = string.Empty;

            if (scheduleDtoLookup.TryGetValue(job.ScheduleId, out var scheduleDto))
            {
                scheduleName = scheduleDto.Name;
            }

            return new SyncJobStatusDto
            {
                Id = job.Id,
                BatchId = job.BatchId,
                ScheduleId = job.ScheduleId,
                VenueName = job.VenueName,
                ScheduleName = scheduleName,
                Status = job.Status,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                CandidateCount = job.CandidateCount
            };
        });

        // Update papers statuses after successful syncing
        await _commonService
            ._unitOfWork
            .Repository<PaperMetadata, long>()
            .Query()
            .Where(p => paperIds.Contains(p.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.PaperStatus, AvailabilityStatus.Synced), cancellationToken: cancellationToken);

        // Update papers forms statuses after successful syncing
        await _commonService
            ._unitOfWork
            .Repository<PaperForm, long>()
            .Query()
            .Where(pf => paperIds.Contains(pf.PaperId.Value) && (!formId.HasValue || pf.Id == formId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(pf => pf.FormStatus, AvailabilityStatus.Synced), cancellationToken: cancellationToken);

        return _commonService._apiResponse.GetApiResponse(
            CustomCodeStatus.Success,
            HttpStatusCode.Accepted,
            Resource.syncjobs,
            jobDtos
        );
    }


    #region Data Fetching Methods

    private async Task<List<SyncJob>> GetSyncJobsAsync(
        long scheduleId,
        List<long> venueIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _commonService
            ._unitOfWork
            .Repository<ScheduleVenue, long>()
            .Query()
            .Where(sv => sv.ScheduleId == scheduleId &&
                         !sv.Venue.IsDeleted &&
                         sv.ScheduleMetadata.PublishingStatus == PublishingStatus.Published &&
                         !sv.ScheduleMetadata.IsDeleted);

        if (venueIds != null && venueIds.Any())
        {
            query = query.Where(sv => venueIds.Contains(sv.VenueId));
        }

        return await query
            .Select(sv => new SyncJob(sv.ScheduleId, sv.VenueId))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<long, string>> GetVenueUrlsAsync(List<long> venueIds, CancellationToken cancellationToken = default)
    {
        return await _commonService
            ._unitOfWork
            .Repository<Venue, long>()
            .Query()
            .Where(v => venueIds.Contains(v.Id) && !string.IsNullOrWhiteSpace(v.Url))
            .AsNoTracking()
            .ToDictionaryAsync(v => v.Id, v => v.Url, cancellationToken);
    }

    private async Task<Dictionary<long, ScheduleSyncResponseDto>> FetchScheduleDataAsync(List<long> scheduleIds, CancellationToken cancellationToken = default)
    {
        var schedules = await _parallelQueryService.ExecuteReadAsync<ScheduleMetadata, long, List<ScheduleMetadata>>(
             repository => repository
                .Query()
                .Include(s => s.SecurityConfiguration)
                .Include(s => s.Languages).ThenInclude(l => l.Language)
                .Where(s => scheduleIds.Contains(s.Id))
                .AsNoTracking()
                .ToListAsync(cancellationToken)
        );

        var scheduleDtos = schedules.ConvertAll(s => new ScheduleSyncResponseDto
        {
            OriginalScheduleId = s.Id,
            Name = s.Name,
            Description = s.Description,
            Code = s.Code,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            SecurityConfiguration = JsonSerializer.Serialize(_commonService._mapper.Map<ScheduleSecurityConfigurationDto>(s.SecurityConfiguration)),
            Languages = JsonSerializer.Serialize(s.Languages.Select(l => _commonService._mapper.Map<LanguageDto>(l.Language)).ToList())
        });

        return scheduleDtos.ToDictionary(s => s.OriginalScheduleId);
    }

    private async Task<(ILookup<long, PaperSyncResponseDto> papers, List<long> paperIds, List<long> schedulePapersIds)> FetchPaperDataAsync(
        long scheduleId,
        long? paperId = null,
        CancellationToken cancellationToken = default
    )
    {
        var today = DateOnly.FromDateTime(DateTimeHelper.Now);

        var schedulePapers = _parallelQueryService.ExecuteReadAsync<SchedulePaper, long, List<SchedulePaper>>(
            repository => repository
                .Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => p.ScheduleMetadataId == scheduleId && (!paperId.HasValue || p.PaperId == paperId.Value) && p.EndDate >= today)
            .Include(sp => sp.PaperMetadata).ThenInclude(p => p.MarkingScheme)
            .Include(sp => sp.PaperMetadata).ThenInclude(p => p.Language)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.InstructionTemplate)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.OtherInstructionTemplate)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.DisclaimerTemplate)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.CertificateTemplate)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.ResultTemplate)
            .Include(sp => sp.PaperSettings).ThenInclude(ps => ps.RuleTemplate)
            .ToListAsync(cancellationToken)
        );

        var mainPapers = await schedulePapers;

        if (mainPapers.Count == 0)
        {
            return (Enumerable.Empty<PaperSyncResponseDto>().ToLookup(p => p.ScheduleId), new List<long>(), new List<long>());
        }

        var schedulePapersIds = mainPapers.ConvertAll(p => p.Id);

        var paperIds = mainPapers.Select(p => p.PaperId).Distinct().ToList();

        var adaptivePapers = mainPapers.Where(p => p.PaperMetadata.Type == PaperType.Adaptive).ToList();

        var transitionProfileIds = adaptivePapers
            .Where(p => p.PaperMetadata.TransitionProfileId.HasValue)
            .Select(p => p.PaperMetadata.TransitionProfileId!.Value)
            .Distinct()
            .ToList();

        var transitionProfiles = await _commonService
            ._unitOfWork
            .Repository<TransitionProfile, long>()
            .Query()
            .AsNoTracking()
            .Where(tp => transitionProfileIds.Contains(tp.Id))
            .Include(tp => tp.TransitionLevels).ThenInclude(tl => tl.QuestionCategory)
            .Include(tp => tp.TransitionLevels).ThenInclude(tl => tl.DifficultyLevel).ThenInclude(dl => dl.DeltaType)
            .ToListAsync(cancellationToken);

        var transitionProfilesDict = transitionProfiles.ToDictionary(tp => tp.Id);

        var difficultyProfileIdsFromTransition = transitionProfiles
            .Select(tp => tp.DifficultyProfileId)
            .Distinct()
            .ToList();

        var allDifficultyProfileIds = difficultyProfileIdsFromTransition
            .Distinct()
            .ToList();

        var difficultyProfilesTask = _parallelQueryService.ExecuteReadAsync<DifficultyProfile, long, List<DifficultyProfile>>(
            repository => repository
                .Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(dp => allDifficultyProfileIds.Contains(dp.Id))
                .Include(dp => dp.DifficultyLevels)
                .ToListAsync(cancellationToken)
        );

        var subjectsTask = _parallelQueryService.ExecuteReadAsync<PaperSubject, long, List<PaperSubject>>(
            repository => repository
           .Query()
           .AsNoTracking()
           .Where(ps => paperIds.Contains(ps.PaperId))
           .Include(ps => ps.Subject)
           .ToListAsync(cancellationToken)
        );

        var adaptivePaperIds = adaptivePapers
            .Where(p => p.PaperMetadata.DPathCalculationMode == DPathCalculationMode.ManualFinalScore)
            .Select(p => p.PaperId)
            .Distinct()
            .ToList();

        Task<List<PaperStageCategoryDecisionPath>> decisionPathsTask = null;
        ILookup<long, PaperStageCategoryDecisionPath> decisionPathsLookup = null;

        if (adaptivePaperIds.Count > 0)
        {
            decisionPathsTask = _parallelQueryService.ExecuteReadAsync<PaperStageCategoryDecisionPath, long, List<PaperStageCategoryDecisionPath>>(
                repository => repository
                    .Query()
                    .AsNoTracking()
                    .Where(x => adaptivePaperIds.Contains(x.PaperId) && !x.StageId.HasValue && x.FixedDPath.HasValue)
                    .ToListAsync(cancellationToken)
            );

            await Task.WhenAll(subjectsTask, difficultyProfilesTask, decisionPathsTask);

            decisionPathsLookup = (await decisionPathsTask).ToLookup(x => x.PaperId);
        }
        else
        {
            await Task.WhenAll(subjectsTask, difficultyProfilesTask);

            decisionPathsLookup = Enumerable.Empty<PaperStageCategoryDecisionPath>().ToLookup(x => x.PaperId);
        }

        var subjectsLookup = (await subjectsTask).ToLookup(s => s.PaperId);

        var difficultyProfilesDict = (await difficultyProfilesTask).ToDictionary(dp => dp.Id);

        var paperDtos = mainPapers.ConvertAll(p =>
        {
            var paperSubjects = subjectsLookup[p.PaperId].Select(ps => ps.Subject).ToList();

            var subjectsDtoList = paperSubjects.ConvertAll(s => _commonService._mapper.Map<PaperSubjectDto>(s));

            var settingsDto = _commonService._mapper.Map<PaperSettingsDto>(p.PaperSettings);

            string transitionProfileJson = null;
            string questionCategoriesJson = null;
            TransitionProfile transitionProfile = null;

            var dPathMode = p.PaperMetadata.Type == PaperType.Standard
               ? DPathCalculationMode.None
               : p.PaperMetadata.DPathCalculationMode;

            var adaptiveOrder = p.PaperMetadata.Type == PaperType.Adaptive
               ? p.PaperMetadata.AdaptiveCategoryExecutionOrder
               : null;

            var adaptiveSubtype = p.PaperMetadata.Type == PaperType.Adaptive
               ? p.PaperMetadata.AdaptiveSubtype
               : AdaptivePaperSubtype.None;

            if (p.PaperMetadata.Type == PaperType.Adaptive &&
                p.PaperMetadata.TransitionProfileId.HasValue &&
                transitionProfilesDict.TryGetValue(p.PaperMetadata.TransitionProfileId.Value, out transitionProfile)
            )
            {
                transitionProfileJson = JsonSerializer.Serialize(new
                {
                    transitionProfile.Id,
                    transitionProfile.Name,
                    transitionProfile.DifficultyProfileId,
                    Levels = transitionProfile.TransitionLevels.Select(l => new
                    {
                        l.Id,
                        l.Name,
                        l.UpperDScore,
                        l.LowerDScore,
                        l.DifficultyLevelId,
                        l.QuestionCategoryId,
                        DeltaTypeName = l.DifficultyLevel?.DeltaType?.Name
                    })
                });

                var paperSpecificPaths = decisionPathsLookup[p.PaperId];

                var categories = transitionProfile.TransitionLevels
                    .Select(l => l.QuestionCategory)
                    .Where(c => c != null)
                    .DistinctBy(c => c.Id)
                    .Select(c =>
                    {
                        var fixedPathValue = paperSpecificPaths
                            .FirstOrDefault(path => path.QuestionCategoryId == c.Id)?.FixedDPath;

                        return new
                        {
                            c.Id,
                            c.Name,
                            c.StandardDeviation,
                            c.StandardError1,
                            c.StandardError2,
                            c.BaseValue1,
                            c.BaseValue2,
                            FixedDPath = fixedPathValue
                        };
                    })
                .ToList();

                if (categories.Count > 0)
                {
                    questionCategoriesJson = JsonSerializer.Serialize(categories);
                }
            }

            long? targetDifficultyProfileId = null;

            if (p.PaperMetadata.Type == PaperType.Adaptive && transitionProfile != null)
            {
                targetDifficultyProfileId = transitionProfile.DifficultyProfileId;
            }

            string difficultyProfileJson = null;

            if (targetDifficultyProfileId.HasValue && difficultyProfilesDict.TryGetValue(targetDifficultyProfileId.Value, out var difficultyProfile))
            {
                difficultyProfileJson = JsonSerializer.Serialize(new
                {
                    difficultyProfile.Id,
                    difficultyProfile.Name,
                    Levels = difficultyProfile.DifficultyLevels.Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.FromDelta,
                        d.ToDelta
                    })
                });
            }

            return new PaperSyncResponseDto
            {
                OriginalSchedulePaperId = p.Id,
                OriginalPaperId = p.PaperId,
                ScheduleId = p.ScheduleMetadataId,
                Type = p.PaperMetadata.Type.ToString(),
                AdaptiveSubtype = adaptiveSubtype.ToString(),
                DPathCalculationMode = dPathMode.ToString(),
                Name = p.PaperMetadata.Name,
                Language = p.PaperMetadata.Language?.Name ?? string.Empty,
                LanguageDirection = p.PaperMetadata.Language?.LanguageDirection ?? string.Empty,
                PaperDescription = p.PaperMetadata.Description,
                Code = p.PaperMetadata.Code,
                PaperStatus = AvailabilityStatus.Synced,
                Abbreviation = p.PaperMetadata.Abbreviation,
                Duration = p.PaperMetadata.Duration,
                QuestionsCount = p.PaperMetadata.QuestionsCount,
                AllowInstantResult = p.PaperMetadata.AllowInstantResult,
                TotalMarks = p.PaperMetadata.TotalMarks,
                UsesExcelQuestionsImport = p.PaperMetadata.UsesExcelQuestionsImport,
                QuestionContentType = p.PaperMetadata.QuestionContentType.ToString(),
                SchedulePaperDescription = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                PaperSettings = JsonSerializer.Serialize(settingsDto),
                Subjects = JsonSerializer.Serialize(subjectsDtoList),
                TransitionProfile = transitionProfileJson,
                DifficultyProfile = difficultyProfileJson,
                QuestionCategories = questionCategoriesJson,
                // TODO: Should be revised later
                MarkingScheme = "{}",
                AdaptiveCategoryExecutionOrder = adaptiveOrder
            };
        });

        var lookup = paperDtos.ToLookup(p => p.ScheduleId);

        return (lookup, paperIds, schedulePapersIds);
    }

    private async Task<(ILookup<long, PaperFormSyncResponseDto> forms, List<long> formIds)> FetchFormDataAsync(List<long> paperIds, List<long> schedulePapersIds, long? formId = null, CancellationToken cancellationToken = default)
    {
        var formsIds = await _commonService._unitOfWork
              .Repository<SchedulePaperForms, long>()
              .Query()
              .Where(f => schedulePapersIds.Contains(f.SchedulePaperId))
              .Select(x => x.FormId)
              .ToListAsync(cancellationToken);

        List<PaperForm> forms;

        if (formsIds.Count > 0)
        {
            forms = await _commonService._unitOfWork
                  .Repository<PaperForm, long>()
                  .Query()
                  .Where(f => f.PaperId.HasValue && formsIds.Contains(f.Id) && (!formId.HasValue || f.Id == formId.Value))
                  .AsNoTracking()
                  .ToListAsync(cancellationToken);
        }
        else
        {
            forms = await _commonService._unitOfWork
                 .Repository<PaperForm, long>()
                 .Query()
                 .Where(f => f.PaperId.HasValue && paperIds.Contains(f.PaperId.Value))
                 .AsNoTracking()
                 .ToListAsync(cancellationToken);
        }

        var formDtos = forms.ConvertAll(f => new PaperFormSyncResponseDto
        {
            OriginalPaperFormId = f.Id,
            Name = f.Name,
            PaperId = f.PaperId.Value
        });

        var formIds = formDtos.ConvertAll(f => f.OriginalPaperFormId);
        var lookup = formDtos.ToLookup(f => f.PaperId);

        return (lookup, formIds);
    }

    private async Task<ILookup<long, SectionSyncResponseDto>> FetchSectionDataAsync(
       List<long> paperIds,
       List<long> formIds,
       Dictionary<long, long> formToPaperMap,
       CancellationToken cancellationToken = default
    )
    {
        var paperToFormsMap = formToPaperMap
            .GroupBy(x => x.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList());

        var standardSectionsTask = _parallelQueryService.ExecuteReadAsync<StandardSection, long, List<(long PaperId, long? FormId, SectionSyncResponseDto Dto, PaperTemplateDto TemplateDto)>>(
            repo => repo
                .Query()
                .Include(s => s.InstructionSectionTemplate)
                .AsNoTracking()
                .Where(s => paperIds.Contains(s.PaperId))
                .Select(s => new ValueTuple<long, long?, SectionSyncResponseDto, PaperTemplateDto>(
                    s.PaperId,
                    s.FormId, // This is null when the paper is standard auto, because the concept of MODELING
                    new SectionSyncResponseDto
                    {
                        OriginalSectionId = s.Id,
                        Name = s.Name,
                        OrderId = s.OrderId,
                        TimeInMinutes = s.TimeInMinutes,
                        Type = PaperType.Standard.ToString(),
                        AdaptivePaperSubtype = AdaptivePaperSubtype.None.ToString(),
                        PaperFormId = s.FormId ?? 0,
                        StageId = null,
                        IsRestrictedTime = s.IsRestrictedTime,
                        IsRandom = s.IsRandom,
                        InstructionSectionTemplate = null
                    },
                    s.InstructionSectionTemplate != null
                        ? new PaperTemplateDto(s.InstructionSectionTemplate.Id, s.InstructionSectionTemplate.Name, s.InstructionSectionTemplate.Content)
                        : null))
                .ToListAsync(cancellationToken)
        );

        var stages = await _commonService
            ._unitOfWork
            .Repository<Stage, long>()
            .Query()
            .AsNoTracking()
            .Where(s => s.FormId.HasValue && formIds.Contains(s.FormId.Value))
            .Select(s => new
            {
                StageId = s.Id,
                FormId = s.FormId.Value
            })
            .ToListAsync(cancellationToken);

        var stageTuples = stages.ConvertAll(s => (s.StageId, s.FormId));

        var stageIds = stageTuples.ConvertAll(s => s.StageId);

        var stageToFormMap = stageTuples.ToDictionary(s => s.StageId, s => s.FormId);

        var adaptiveSectionsTask = _parallelQueryService.ExecuteReadAsync<AdaptiveSection, long, List<(long StageId, SectionSyncResponseDto Dto, PaperTemplateDto? TemplateDto)>>(
            repo => repo.Query()
                .Include(s => s.InstructionSectionTemplate)
                .AsNoTracking()
                .Where(s => stageIds.Contains(s.StageId))
                .Select(s => new ValueTuple<long, SectionSyncResponseDto, PaperTemplateDto?>(s.StageId, new SectionSyncResponseDto
                {
                    OriginalSectionId = s.Id,
                    Name = s.Name,
                    OrderId = s.Order,
                    TimeInMinutes = s.TimeInMinutes,
                    Type = PaperType.Adaptive.ToString(),
                    AdaptivePaperSubtype = s.AdaptivePaperSubtype.ToString(),
                    PaperFormId = 0,
                    StageId = s.StageId,
                    UnScored = s.UnScored,
                    DifficultyLevelId = s.DifficultyLevelId,
                    InstructionSectionTemplate = null
                },
                s.InstructionSectionTemplate != null
                    ? new PaperTemplateDto(s.InstructionSectionTemplate.Id, s.InstructionSectionTemplate.Name, s.InstructionSectionTemplate.Content)
                    : null))
                .ToListAsync(cancellationToken)
        );

        await Task.WhenAll(standardSectionsTask, adaptiveSectionsTask);

        var standardResults = await standardSectionsTask;
        var adaptiveResults = await adaptiveSectionsTask;

        var finalResults = new List<(long PaperId, SectionSyncResponseDto Dto)>();

        foreach (var (pId, sectionFormId, dto, templateDto) in standardResults)
        {
            if (templateDto != null)
            {
                dto.InstructionSectionTemplate = JsonSerializer.Serialize(templateDto);
            }

            if (sectionFormId.HasValue) // Then this is a standard manual section
            {
                if (formIds.Contains(sectionFormId.Value))
                {
                    dto.PaperFormId = sectionFormId.Value;
                    finalResults.Add((pId, dto));
                }
            }
            else // Then this is a standard auto section OR adaptive section
            {
                if (paperToFormsMap.TryGetValue(pId, out var fIds))
                {
                    foreach (var fId in fIds) // Here we link the auto/adaptive sections to their forms
                    {
                        var newDto = new SectionSyncResponseDto
                        {
                            OriginalSectionId = dto.OriginalSectionId,
                            Name = dto.Name,
                            OrderId = dto.OrderId,
                            TimeInMinutes = dto.TimeInMinutes,
                            Type = dto.Type,
                            AdaptivePaperSubtype = dto.AdaptivePaperSubtype,
                            PaperFormId = fId,
                            StageId = dto.StageId,
                            IsRestrictedTime = dto.IsRestrictedTime,
                            IsRandom = dto.IsRandom,
                            UnScored = dto.UnScored,
                            DifficultyLevelId = dto.DifficultyLevelId,
                            InstructionSectionTemplate = dto.InstructionSectionTemplate
                        };

                        finalResults.Add((pId, newDto));
                    }
                }
            }
        }

        foreach (var item in adaptiveResults)
        {
            if (item.TemplateDto != null)
            {
                item.Dto.InstructionSectionTemplate = JsonSerializer.Serialize(item.TemplateDto);
            }

            if (stageToFormMap.TryGetValue(item.StageId, out var fId))
            {
                item.Dto.PaperFormId = fId;

                if (formToPaperMap.TryGetValue(fId, out var pId))
                {
                    finalResults.Add((pId, item.Dto));
                }
            }
        }

        return finalResults.ToLookup(x => x.PaperId, x => x.Dto);
    }

    private async Task<ILookup<long, StageSyncResponseDto>> FetchStageDataAsync(
        List<long> formIds,
        CancellationToken cancellationToken = default
    )
    {
        var stagesWithPaths = await _commonService
            ._unitOfWork
            .Repository<Stage, long>()
            .Query()
            .AsNoTracking()
            .Where(s => s.FormId.HasValue && formIds.Contains(s.FormId.Value))
            .Include(s => s.InstructionSectionTemplate)
            .Include(s => s.CategoryDecisionPaths)
              .ThenInclude(p => p.QuestionCategory)
            .Select(s => new
            {
                Stage = s,
                InstructionTemplate = s.InstructionSectionTemplate,
                Paths = s.CategoryDecisionPaths
                    .Select(p => new StageCategoryDecisionPathDto(
                        p.Id,
                        p.QuestionCategoryId,
                        p.QuestionCategory.Name,
                        p.DecisionPathValue
                    ))
                    .ToList()
            })
            .ToListAsync(cancellationToken);


        var stageDtos = stagesWithPaths.ConvertAll(group =>
        {
            var s = group.Stage;
            var paths = group.Paths;
            var template = group.InstructionTemplate;

            return new StageSyncResponseDto
            {
                OriginalStageId = s.Id,
                Name = s.Name,
                RenderedPartName = s.RenderedPartName,
                Order = s.Order,
                TimeInMinutes = s.TimeInMinutes,
                PaperFormId = s.FormId!.Value,
                DecisionPaths = paths.Count > 0 ? JsonSerializer.Serialize(paths) : null,
                InstructionSectionTemplate = template != null
                    ? JsonSerializer.Serialize(new PaperTemplateDto(
                        s.InstructionSectionTemplate.Id,
                        s.InstructionSectionTemplate.Name,
                        s.InstructionSectionTemplate.Content))
                    : null
            };
        });

        return stageDtos.ToLookup(s => s.PaperFormId);
    }

    private async Task<ILookup<long, BlockSyncResponseDto>> FetchBlockDataAsync(
        List<long> formIds,
        CancellationToken cancellationToken = default
    )
    {
        var blocks = await _commonService
            ._unitOfWork
            .Repository<PaperFormBlock, long>()
            .Query()
            .AsNoTracking()
            .Where(pfb => formIds.Contains(pfb.FormId) && pfb.AdaptiveSectionId.HasValue)
            .Include(pfb => pfb.Block)
            .Select(pfb => new BlockSyncResponseDto
            {
                OriginalBlockId = pfb.BlockId,
                Name = pfb.Block.Name,
                Code = pfb.Block.Code,
                DifficultyLevelId = pfb.Block.DifficultyLevelId,
                QuestionCategoryId = pfb.Block.QuestionCategoryId,
                OriginalSectionId = pfb.AdaptiveSectionId.Value,
                PaperFormId = pfb.FormId
            })
            .ToListAsync(cancellationToken);

        return blocks.ToLookup(b => b.PaperFormId);
    }

    private async Task<(ILookup<long, QuestionSyncResponseDto> lookup, string? errorMessage)> FetchQuestionDataAsync(
        List<long> formIds,
        CancellationToken cancellationToken = default
    )
    {
        var formPaperTypesTask = _parallelQueryService.ExecuteReadAsync<PaperForm, long, List<(long FormId, PaperType PaperType, long PaperId)>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(f => formIds.Contains(f.Id))
                .Select(f => new ValueTuple<long, PaperType, long>(
                    f.Id,
                    f.Paper.Type,
                    f.PaperId.Value
                 ))
                .ToListAsync(cancellationToken)
        );

        var formPaperTypes = await formPaperTypesTask;

        var standardFormIds = formPaperTypes.Where(x => x.PaperType == PaperType.Standard).Select(x => x.FormId).ToList();

        var adaptiveFormIds = formPaperTypes.Where(x => x.PaperType == PaperType.Adaptive).Select(x => x.FormId).ToList();

        // ========== Separate spine data for standard and adaptive ==========
        List<FormQuestionSpineInternal> standardSpineData = [];
        List<FormQuestionSpineInternal> adaptiveSpineData = [];

        if (standardFormIds.Count > 0)
        {
            var standardSpineTask = _parallelQueryService.ExecuteReadAsync<GeneratedFormQuestion, long, List<FormQuestionSpineInternal>>(
                repo => repo
                    .Query()
                    .AsNoTracking()
                    .Where(fq => fq.FormId.HasValue && standardFormIds.Contains(fq.FormId.Value))
                    .Select(fq => new FormQuestionSpineInternal
                    {
                        FormId = fq.FormId.Value,
                        QuestionId = fq.QuestionId.Value,
                        QuestionTypeId = fq.Question.QuestionTypeId,
                        FormQuestionStatus = fq.FormQuestionStatus,
                        DifficultyLevelId = fq.Question.DifficultyLevelId,
                        Score = fq.Score,
                        PaperId = fq.Form.PaperId.Value,
                        PaperLanguageId = fq.Form.Paper.LanguageId,
                        PaperSubType = fq.Form.Paper.QuestionSelectionType,
                        BlockId = null
                    })
                    .ToListAsync(cancellationToken)
            );

            // DistinctBy only for standard to avoid duplicates
            standardSpineData = [.. (await standardSpineTask).DistinctBy(x => new { x.FormId, x.QuestionId })];
        }

        if (adaptiveFormIds.Count > 0)
        {
            var adaptiveSpineTask = _parallelQueryService.ExecuteReadAsync<PaperFormBlock, long, List<FormQuestionSpineInternal>>(
                repo => repo.Query()
                    .AsNoTracking()
                    .Where(pfb => adaptiveFormIds.Contains(pfb.FormId))
                    .SelectMany(pfb => pfb.Block.Questions,
                        (pfb, bq) => new FormQuestionSpineInternal
                        {
                            FormId = pfb.FormId,
                            QuestionId = bq.QuestionMetadataId,
                            BlockId = bq.BlockId, // Carry BlockId for each question-block combination
                            Score = null,
                            PaperId = pfb.PaperId,
                            PaperLanguageId = pfb.Paper.LanguageId,
                            PaperSubType = QuestionSelectionType.Auto
                        })
                    .ToListAsync(cancellationToken)
            );

            // No DistinctBy for adaptive — preserve all question-block combinations
            adaptiveSpineData = await adaptiveSpineTask;
        }

        // Combine both lists
        var spineData = standardSpineData.Concat(adaptiveSpineData).ToList();

        if (spineData.Count == 0)
        {
            return (Enumerable.Empty<QuestionSyncResponseDto>().ToLookup(x => x.PaperFormId), null);
        }

        var rootQuestionIds = spineData.ConvertAll(x => x.QuestionId).Distinct().ToList();

        var questionBlockMapTask = _parallelQueryService.ExecuteReadAsync<PaperFormBlock, long, List<(long FormId, long QuestionId, long BlockId)>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(pfb => formIds.Contains(pfb.FormId))
                .SelectMany(pfb => pfb.Block.Questions,
                    (pfb, bq) => new
                    {
                        pfb.FormId,
                        bq.QuestionMetadataId,
                        bq.BlockId
                    })
                .Where(x => rootQuestionIds.Contains(x.QuestionMetadataId))
                .Select(x => new ValueTuple<long, long, long>(x.FormId, x.QuestionMetadataId, x.BlockId))
                .ToListAsync(cancellationToken)
        );

        var paperIds = spineData.ConvertAll(x => x.PaperId).Distinct();

        var rootsTask = _parallelQueryService.ExecuteReadAsync<QuestionMetadata, long, List<QuestionBaseInternal>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(q => rootQuestionIds.Contains(q.Id))
                .Select(q => new QuestionBaseInternal
                {
                    Id = q.Id,
                    Code = q.Code,
                    Delta = q.Delta,
                    TypeName = q.QuestionType.Name,
                    IsAutoCorrectable = q.QuestionType.IsAutoCorrectable,
                    LayoutName = q.QLayout != null ? q.QLayout.Name : "",
                    SubjectName = q.Subject != null ? q.Subject.Name : "",
                    ScientificEditorPanelEnabled = q.ScientificEditorPanelEnabled,
                    FileManagerEditorPanelEnabled = q.FileManagerEditorPanelEnabled,
                    FileUploadSettings = q.FileUploadResponseSettings == null ? null : new FileUploadSettingsInternal
                    {
                        ShowAnswerTextArea = q.FileUploadResponseSettings.ShowAnswerTextArea,
                        SupportedFileExtensions = q.FileUploadResponseSettings.SupportedFileExtensions,
                        UploadedFilesCount = q.FileUploadResponseSettings.UploadedFilesCount,
                        SingleFileMaxSizeInMB = q.FileUploadResponseSettings.SingleFileMaxSizeInMB,
                        QuestionMetadataId = q.FileUploadResponseSettings.QuestionMetadataId
                    },
                    SubQuestionIds = q.SubQuestions.OrderBy(sq => sq.Id).Select(sq => sq.Id).ToList()
                })
                .ToListAsync(cancellationToken)
        );

        var detailsTask = _parallelQueryService.ExecuteReadAsync<QuestionDetails, long, List<QuestionDetailsInternal>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(qd => rootQuestionIds.Contains(qd.QuestionMetadataId))
                .Select(qd => new QuestionDetailsInternal
                {
                    QuestionId = qd.QuestionMetadataId,
                    Body = qd.Body,
                    AttachmentFileName = qd.AttachmentFileName,
                    Instructions = qd.Instructions,
                    ModelAnswer = qd.ModelAnswer,
                    HasShuffled = qd.HasShuffled,
                    MaxWords = qd.MaxWords,
                    LanguageName = qd.Language.Name,
                    LanguageId = qd.LanguageId,
                    MaxRecordingTimeInSeconds = qd.MaxRecordingTimeInSeconds,
                    UseArabicNumbers = qd.UseArabicNumbers,
                    MatchingPairQuestionItems = qd.MatchingPairQuestionItems.Select(m => new MatchingPairQuestionItemInternal
                    {
                        Id = m.Id,
                        Body = m.Body,
                        ColumnOrder = m.ColumnOrder,
                        QuestionDetailsId = m.QuestionDetailsId,
                        IsDataSource = m.IsDataSource
                    }).ToList(),
                    Choices = qd.QuestionsChoices.Select(c => new ChoiceInternal
                    {
                        Id = c.Id,
                        ChoiceText = c.ChoiceText,
                        IsCorrectAnswer = c.IsCorrectAnswer,
                        AttachmentFileName = c.AttachmentFileName,
                        ChoiceOrderId = c.OrderId
                    }).ToList(),
                    SegmentQuestionProperties = qd.SegmentQuestionProperties == null
                        ? null
                        : new SyncSegmentPropertiesDto
                        {
                            Id = qd.SegmentQuestionProperties.Id,
                            ThinkingTime = qd.SegmentQuestionProperties.ThinkingTime,
                            ResponseTime = qd.SegmentQuestionProperties.ResponseTime,
                            WordsCount = qd.SegmentQuestionProperties.WordsCount,
                            OrderNumber = qd.SegmentQuestionProperties.OrderNumber,
                            HasScore = qd.SegmentQuestionProperties.HasScore,
                            SegmentAudioUrl = qd.SegmentQuestionProperties.SegmentAudioUrl,
                            SegmentQuestionResponseType = qd.SegmentQuestionProperties.SegmentQuestionResponseType
                        }
                })
                .ToListAsync(cancellationToken)
        );

        var filesTask = _parallelQueryService.ExecuteReadAsync<QuestionDocLibFile, long, List<FileInternal>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(f => rootQuestionIds.Contains(f.QuestionId))
                .Select(f => new FileInternal { QuestionId = f.QuestionId, FileId = f.FileId })
                .ToListAsync(cancellationToken)
        );

        var manualMapTask = _parallelQueryService.ExecuteReadAsync<ManualPaperItemBankQuestionSection, long, List<SectionMapInternal>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(m => rootQuestionIds.Contains(m.QuestionMetaDataId) && paperIds.Contains(m.ItemBankPoint.PaperId))
                .Select(m => new SectionMapInternal
                {
                    QuestionId = m.QuestionMetaDataId,
                    PaperId = m.ItemBankPoint.PaperId,
                    SectionId = m.SectionId,
                    FormId = m.Section == null ? null : m.Section.FormId,
                })
                .ToListAsync(cancellationToken)
        );


        var autoMapTask = _parallelQueryService.ExecuteReadAsync<AutoPaperItemBankQuestionSection, long, List<AutoSectionRawInternal>>(
            repo => repo.Query()
                .AsNoTracking()
                .Where(a => paperIds.Contains(a.ItemBankPoint.PaperId))
                .Select(a => new AutoSectionRawInternal
                {
                    PaperId = a.ItemBankPoint.PaperId,
                    SectionId = a.SectionId,
                    QuestionTypeId = a.QuestionTypeID,
                    DifficultyLevelId = a.DifficultyLevelID,
                    SelectedCount = a.SelectedCount,
                    QuestionIdsString = a.QuestionIds
                })
                .ToListAsync(cancellationToken)
        );

        await Task.WhenAll(rootsTask, detailsTask, filesTask, manualMapTask, autoMapTask, questionBlockMapTask);

        var roots = await rootsTask;
        var rootsDict = roots.ToDictionary(x => x.Id);

        var subQuestionIds = roots.SelectMany(r => r.SubQuestionIds).ToHashSet();
        spineData = spineData.Where(s => !subQuestionIds.Contains(s.QuestionId)).ToList();

        var detailsLookup = (await detailsTask).ToLookup(x => x.QuestionId);
        var filesLookup = (await filesTask).ToLookup(x => x.QuestionId);

        var invalidQuestionCodes = new List<string>();

        foreach (var questionId in rootQuestionIds)
        {
            var fileIdSet = filesLookup[questionId].Select(f => f.FileId.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool hasMissingMedia = detailsLookup[questionId].Any(detail =>
                (!string.IsNullOrEmpty(detail.AttachmentFileName) && !fileIdSet.Contains(detail.AttachmentFileName)) ||
                detail.Choices.Any(c => !string.IsNullOrEmpty(c.AttachmentFileName) && !fileIdSet.Contains(c.AttachmentFileName))
            );

            if (hasMissingMedia && rootsDict.TryGetValue(questionId, out var qBase))
            {
                invalidQuestionCodes.Add(qBase.Code);
            }
        }

        var manualLookup = (await manualMapTask).ToLookup(x => (x.QuestionId, x.FormId, x.PaperId));

        var autoRawData = await autoMapTask;
        var autoLookup = new Dictionary<(long QuestionId, long FormId, long PaperId), (long SectionId, PaperQuestionStatus FormQuestionStatus)>();
        var autoForms = spineData.Where(fq => fq.PaperSubType == QuestionSelectionType.Auto).GroupBy(fq => fq.FormId); // Group auto form-questions by form id

        foreach (var autoForm in autoForms) // Loop on these forms to apply the AUTO PAPER MODELING on them
        {
            var currentFormId = autoForm.Key;
            var allAutoFormQuestions = autoForm.ToList();

            foreach (var row in autoRawData) // Loop on the AUTO PAPER MODELING RECORDS to apply each record on the current form
            {
                var cleaned = row.QuestionIdsString.Trim('[', ']');

                var qIds = cleaned.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var qIdStr in qIds)
                {
                    if (long.TryParse(qIdStr, out long parsedQId))
                    {
                        var targetFormQuestion = allAutoFormQuestions.Find(fq => fq.QuestionId == parsedQId);

                        if (targetFormQuestion == null) continue;

                        autoLookup[(parsedQId, currentFormId, row.PaperId)] = (row.SectionId ?? 0, targetFormQuestion.FormQuestionStatus);

                        allAutoFormQuestions.Remove(targetFormQuestion); // Exclude that question to avoid adding it again on the same form, but in another section
                    }
                }

                var matchingQuestions = allAutoFormQuestions
                    .Where(fq => fq.QuestionTypeId == row.QuestionTypeId && fq.DifficultyLevelId == row.DifficultyLevelId)
                    .Take((int)row.SelectedCount)
                    .ToList();

                foreach (var autoFormQuestion in matchingQuestions)
                {
                    autoLookup[(autoFormQuestion.QuestionId, currentFormId, autoFormQuestion.PaperId)] = (row.SectionId ?? 0, autoFormQuestion.FormQuestionStatus);

                    allAutoFormQuestions.Remove(autoFormQuestion); // Exclude that question to avoid adding it again on the same form, but in another section
                }
            }
        }

        var allSubIds = roots.SelectMany(r => r.SubQuestionIds).Distinct().ToList();

        var subQuestionsDict = new Dictionary<(long QuestionId, long LanguageId), QuestionSyncResponseDto>();

        if (allSubIds.Count > 0)
        {
            var sqBaseTask = _parallelQueryService.ExecuteReadAsync<QuestionMetadata, long, List<QuestionBaseInternal>>(
                repo => repo.Query()
                .AsNoTracking()
                .Where(q => allSubIds.Contains(q.Id))
                .Select(q => new QuestionBaseInternal
                {
                    Id = q.Id,
                    Code = q.Code,
                    Delta = q.Delta,
                    TypeName = q.QuestionType.Name,
                    IsAutoCorrectable = q.QuestionType.IsAutoCorrectable,
                    LayoutName = q.QLayout != null ? q.QLayout.Name : "",
                    SubjectName = q.Subject != null ? q.Subject.Name : "",
                    ScientificEditorPanelEnabled = q.ScientificEditorPanelEnabled,
                    FileManagerEditorPanelEnabled = q.FileManagerEditorPanelEnabled,
                    FileUploadSettings = q.FileUploadResponseSettings == null ? null : new FileUploadSettingsInternal
                    {
                        ShowAnswerTextArea = q.FileUploadResponseSettings.ShowAnswerTextArea,
                        SupportedFileExtensions = q.FileUploadResponseSettings.SupportedFileExtensions,
                        UploadedFilesCount = q.FileUploadResponseSettings.UploadedFilesCount,
                        SingleFileMaxSizeInMB = q.FileUploadResponseSettings.SingleFileMaxSizeInMB,
                        QuestionMetadataId = q.FileUploadResponseSettings.QuestionMetadataId
                    }
                }).ToListAsync(cancellationToken)
            );

            var sqDetailsTask = _parallelQueryService.ExecuteReadAsync<QuestionDetails, long, List<QuestionDetailsInternal>>(
                repo => repo.Query()
                .AsNoTracking()
                .Where(qd => allSubIds.Contains(qd.QuestionMetadataId))
                .Select(qd => new QuestionDetailsInternal
                {
                    QuestionId = qd.QuestionMetadataId,
                    Body = qd.Body,
                    Instructions = qd.Instructions,
                    AttachmentFileName = qd.AttachmentFileName,
                    ModelAnswer = qd.ModelAnswer,
                    HasShuffled = qd.HasShuffled,
                    MaxWords = qd.MaxWords,
                    LanguageName = qd.Language.Name,
                    LanguageId = qd.LanguageId,
                    MaxRecordingTimeInSeconds = qd.MaxRecordingTimeInSeconds,
                    UseArabicNumbers = qd.UseArabicNumbers,
                    Choices = qd.QuestionsChoices.Select(c => new ChoiceInternal
                    {
                        Id = c.Id,
                        ChoiceText = c.ChoiceText,
                        IsCorrectAnswer = c.IsCorrectAnswer,
                        AttachmentFileName = c.AttachmentFileName,
                        ChoiceOrderId = c.OrderId
                    }).ToList(),
                    MatchingPairQuestionItems = qd.MatchingPairQuestionItems.Select(m => new MatchingPairQuestionItemInternal
                    {
                        Id = m.Id,
                        Body = m.Body,
                        ColumnOrder = m.ColumnOrder,
                        QuestionDetailsId = m.QuestionDetailsId,
                        IsDataSource = m.IsDataSource
                    }).ToList(),
                    SegmentQuestionProperties = qd.SegmentQuestionProperties == null
                        ? null
                        : new SyncSegmentPropertiesDto
                        {
                            Id = qd.SegmentQuestionProperties.Id,
                            ThinkingTime = qd.SegmentQuestionProperties.ThinkingTime,
                            ResponseTime = qd.SegmentQuestionProperties.ResponseTime,
                            WordsCount = qd.SegmentQuestionProperties.WordsCount,
                            OrderNumber = qd.SegmentQuestionProperties.OrderNumber,
                            HasScore = qd.SegmentQuestionProperties.HasScore,
                            SegmentAudioUrl = qd.SegmentQuestionProperties.SegmentAudioUrl,
                            SegmentQuestionResponseType = qd.SegmentQuestionProperties.SegmentQuestionResponseType
                        }
                }).ToListAsync(cancellationToken)
            );

            var sqFilesTask = _parallelQueryService.ExecuteReadAsync<QuestionDocLibFile, long, List<FileInternal>>(
                repo => repo.Query()
                .AsNoTracking()
                .Where(f => allSubIds.Contains(f.QuestionId))
                .Select(f => new FileInternal { QuestionId = f.QuestionId, FileId = f.FileId })
                .ToListAsync(cancellationToken)
            );

            await Task.WhenAll(sqBaseTask, sqDetailsTask, sqFilesTask);

            var sqBase = await sqBaseTask;
            var sqDetails = (await sqDetailsTask).ToLookup(x => x.QuestionId);
            var sqFiles = (await sqFilesTask).ToLookup(x => x.QuestionId);

            foreach (var sq in sqBase)
            {
                var subFileIdSet = sqFiles[sq.Id].Select(f => f.FileId.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool hasMissingMedia = sqDetails[sq.Id].Any(detail =>
                    (!string.IsNullOrEmpty(detail.AttachmentFileName) && !subFileIdSet.Contains(detail.AttachmentFileName)) ||
                    detail.Choices.Any(c => !string.IsNullOrEmpty(c.AttachmentFileName) && !subFileIdSet.Contains(c.AttachmentFileName))
                );

                if (hasMissingMedia)
                {
                    invalidQuestionCodes.Add(sq.Code);
                }
            }

            foreach (var sq in sqBase)
            {
                foreach (var d in sqDetails[sq.Id]) // Loop through all language versions
                {
                    var fs = sqFiles[sq.Id].Select(f => f.FileId).ToList();
                    subQuestionsDict[(sq.Id, d.LanguageId)] = MapToDto(sq, d, fs, null, 0, null, 0, 0);
                }
            }
        }

        var results = new List<QuestionSyncResponseDto>();

        // ========== Move questionBlockLookup outside the loop (performance fix) ==========
        var questionBlockLookup = (await questionBlockMapTask)
            .ToLookup(x => (x.FormId, x.QuestionId), x => x.BlockId);

        foreach (var spine in spineData)
        {
            if (!rootsDict.TryGetValue(spine.QuestionId, out var rootQ)) continue;

            var details = detailsLookup[spine.QuestionId].FirstOrDefault(d => d.LanguageId == spine.PaperLanguageId);
            var files = filesLookup[spine.QuestionId].Select(f => f.FileId).ToList();

            long sectionId = 0;
            string status = null;

            if (spine.PaperSubType == QuestionSelectionType.Manual)
            {
                var secId = manualLookup[(rootQ.Id, spine.FormId, spine.PaperId)].FirstOrDefault()?.SectionId ?? 0;
                var fQStatus = spineData.Find(fq => fq.QuestionId == rootQ.Id && fq.FormId == spine.FormId && fq.PaperId == spine.PaperId)?.FormQuestionStatus.ToString();

                sectionId = secId;
                status = fQStatus;
            }
            else if (spine.PaperSubType == QuestionSelectionType.Auto)
            {
                if (autoLookup.TryGetValue((rootQ.Id, spine.FormId, spine.PaperId), out var result))
                {
                    sectionId = result.SectionId;
                    status = result.FormQuestionStatus.ToString();
                }
                else
                {
                    var matchingRow = autoRawData.FirstOrDefault(a => a.PaperId == spine.PaperId);

                    sectionId = matchingRow?.SectionId ?? 0;

                    status = spine.FormQuestionStatus.ToString();
                }
            }

            // ========== Use spine.BlockId with fallback to questionBlockLookup ==========
            var resolvedBlockId = spine.BlockId ?? questionBlockLookup[(spine.FormId, spine.QuestionId)].FirstOrDefault();

            var rootDto = MapToDto(
                rootQ,
                details,
                files,
                spine.FormId,
                spine.Score ?? 0,
                status,
                sectionId,
                resolvedBlockId);

            rootDto.IsRoot = true;

            // Before: Each sub-question was assigned the full parent score.
            // Now: Each sub-question gets (parent score ÷ number of sub-questions) to keep total marks correct.
            var subCount = rootQ.SubQuestionIds.Count == 0 ? 1 : rootQ.SubQuestionIds.Count;
            var unitScore = (spine.Score ?? 0) / subCount;

            foreach (var subId in rootQ.SubQuestionIds)
            {
                if (subQuestionsDict.TryGetValue((subId, spine.PaperLanguageId), out var subDtoTemplate))
                {
                    var subDto = CloneSubQuestion(
                        subDtoTemplate,
                        rootQ.Id,
                        spine.FormId,
                        sectionId,
                        resolvedBlockId,
                        unitScore
                    );

                    rootDto.SubQuestions.Add(subDto);
                }
            }

            results.Add(rootDto);
        }

        if (invalidQuestionCodes.Count > 0)
        {
            var distinctCodes = string.Join(Environment.NewLine, invalidQuestionCodes.Distinct().Select(c => $"• {c}"));

            return (Enumerable.Empty<QuestionSyncResponseDto>().ToLookup(x => x.PaperFormId), string.Format(Resource.QuestionsMissingMediaFiles, distinctCodes));
        }

        return (results.ToLookup(x => x.PaperFormId), null);
    }

    private async Task<Dictionary<long, DisabilitySyncResponseDto>> FetchDisabilityDataAsync(CancellationToken cancellationToken)
    {
        var disabilities = await _parallelQueryService.ExecuteReadAsync<Disability, long, List<Disability>>(
            repository => repository
                .Query()
                .AsNoTracking()
                .ToListAsync(cancellationToken)
        );

        return disabilities.ToDictionary(
            d => d.Id,
            d => new DisabilitySyncResponseDto
            {
                DisabilityOriginalId = d.Id,
                Name = d.Name,
                Description = d.Description,
                ExtraTimePercentage = d.ExtraTimePercentage
            }
        );
    }

    private async Task<(
        Dictionary<long,
        UserSyncResponseDto> users,
        Dictionary<long, CandidateDetailsSyncResponseDto> details,
        List<CandidatePaperSyncResponseDto> candidatePapers,
        ILookup<(long, long), long> candidateLookup,
        List<long> schedulePaperCandidateIds,
        List<long> candidateIds
    )> FetchCandidateDataAsync(List<long> scheduleIds, List<long> venueIds, List<long> candidateIdsParam = null, CancellationToken cancellationToken = default)
    {
        var schedulePaperCandidates = await _parallelQueryService.ExecuteReadAsync<SchedulePaperCandidate, long, List<SchedulePaperCandidate>>(
            repository => repository
                .Query()
                .Include(spc => spc.SchedulePaper).ThenInclude(sp => sp.PaperMetadata)
                .Include(spc => spc.Candidate).ThenInclude(c => c.Disability)
                .Where(spc => scheduleIds.Contains(spc.SchedulePaper.ScheduleMetadataId) &&
                              (candidateIdsParam == null || candidateIdsParam.Count == 0 || candidateIdsParam.Contains(spc.CandidateId)) &&
                              venueIds.Contains(spc.VenueId) &&
                              (!spc.IsSynced || !spc.Candidate.IsSynced)
                )
                .AsNoTracking()
                .ToListAsync(cancellationToken)
        );

        var schedulePaperCandidateIds = schedulePaperCandidates.ConvertAll(spc => spc.Id);

        var candidateIds = schedulePaperCandidates.Select(spc => spc.CandidateId).Distinct().ToList();

        var uniqueCandidates = schedulePaperCandidates
            .Select(spc => spc.Candidate)
            .DistinctBy(c => c.Id)
            .ToList();

        var userDtosTask = Task.Run(() => uniqueCandidates
            .Select(c => new UserSyncResponseDto
            {
                OriginalUserId = c.Id,
                DisplayName = c.Name,
                Email = c.Email,
                Username = c.UserName,
                Password = c.Password,
                PhoneNumber = c.Mobile,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = _filterParamsValues.UserEmail
            })
            .ToDictionary(u => u.OriginalUserId), cancellationToken);

        var candidateDetailsDtosTask = Task.Run(() => uniqueCandidates
            .Select(c => new CandidateDetailsSyncResponseDto
            {
                UserId = c.Id,
                Code = c.CandidateCode,
                NationalId = c.NationalId,
                Qualification = c.Qualification,
                Gender = c.Gender switch { 0 => "Male", 1 => "Female", _ => "Unknown" },
                DateOfBirth = c.DateOfBirth,
                Address = c.Address,
                PhotoUrl = c.PhotoURL,
                SignatureUrl = c.SignatureURL,
                CenterRegistrationCode = c.RegistrationCenterCode,
                RegistrationDateTime = c.RegistrationDateTime,
                HasDisability = c.HasDisability,
                DisabilityId = c.DisabilityId
            })
            .ToDictionary(cd => cd.UserId), cancellationToken);

        var blockCandidateAnswers = new List<BlockCandidateAnswer>();

        var adaptiveCandidates = schedulePaperCandidates
            .Where(spc => spc.SchedulePaper.PaperMetadata.Type == PaperType.Adaptive)
            .ToList();

        if (adaptiveCandidates.Count > 0)
        {
            var adaptiveCandidateIds = adaptiveCandidates.Select(spc => spc.CandidateId).Distinct().ToList();

            blockCandidateAnswers = await _parallelQueryService.ExecuteReadAsync<BlockCandidateAnswer, long, List<BlockCandidateAnswer>>(
                repository => repository
                    .Query()
                    .Where(bca => adaptiveCandidateIds.Contains(bca.CandidateId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken)
            );
        }

        var candidatePaperDtosTask = Task.Run(() => schedulePaperCandidates
            .ConvertAll(spc => new CandidatePaperSyncResponseDto
            {
                CandidateId = spc.CandidateId,
                SchedulePaperId = spc.SchedulePaperId,
                PaperFormId = spc.PaperFormId ?? 0,
                RegistrationNumber = spc.RegistrationNumber,
                CandidateExamDate = spc.CandidateExamDate,
                VenueId = spc.VenueId,
                AccumulatedSolvedBlockIds = spc.SchedulePaper.PaperMetadata.Type == PaperType.Adaptive && spc.PaperFormId.HasValue
                ? [.. blockCandidateAnswers
                    .Where(bca => bca.CandidateId == spc.CandidateId)
                    .Select(bca => bca.BlockId)
                    .Distinct()]
                : null,
                CandidateExtraTimePercent = spc.Candidate.Disability != null ? spc.Candidate.Disability.ExtraTimePercentage : null
            }), cancellationToken);

        await Task.WhenAll(userDtosTask, candidateDetailsDtosTask, candidatePaperDtosTask);

        var userDtos = await userDtosTask;

        var candidateDetailsDtos = await candidateDetailsDtosTask;

        var candidatePaperDtos = await candidatePaperDtosTask;

        var candidateLookup = schedulePaperCandidates.ToLookup(
            spc => (spc.SchedulePaper.ScheduleMetadataId, spc.VenueId),
            spc => spc.CandidateId
        );

        return (userDtos, candidateDetailsDtos, candidatePaperDtos, candidateLookup, schedulePaperCandidateIds, candidateIds);
    }

    #endregion Data Fetching Methods


    #region Job Processing

    private async Task<JobProcessResult> ProcessSyncJobAsync(
        SyncJob job,
        Guid batchId,
        Dictionary<long, (string Name, string Code)> venueDetailsMap,
        Dictionary<long, ScheduleSyncResponseDto> scheduleDtoLookup,
        ILookup<long, PaperSyncResponseDto> paperDtoLookup,
        ILookup<long, PaperFormSyncResponseDto> formDtoLookup,
        ILookup<long, SectionSyncResponseDto> sectionDtoLookup,
        ILookup<long, StageSyncResponseDto> stageDtoLookup,
        ILookup<long, BlockSyncResponseDto> blockDtoLookup,
        ILookup<long, QuestionSyncResponseDto> questionDtoLookup,
        Dictionary<long, DisabilitySyncResponseDto> disabilityDtoLookup,
        ILookup<(long, long), long> candidateLookup,
        Dictionary<long, UserSyncResponseDto> allUserDtos,
        Dictionary<long, CandidateDetailsSyncResponseDto> allCandidateDetailsDtos,
        List<CandidatePaperSyncResponseDto> candidatePapers,
        bool isPartialSync = false,
        AuditContextDto audit = null,
        CancellationToken cancellationToken = default
    )
    {
        //var pingResult = await PingVenueAsync(
        //    job.VenueId,
        //    venueDetailsMap,
        //    cancellationToken
        //);

        //if (!pingResult.IsSuccess)
        //{
        //    return pingResult;
        //}

        var payload = BuildPayloadForJob(
            job,
            scheduleDtoLookup,
            paperDtoLookup,
            formDtoLookup,
            sectionDtoLookup,
            stageDtoLookup,
            blockDtoLookup,
            questionDtoLookup,
            disabilityDtoLookup,
            candidateLookup,
            allUserDtos,
            allCandidateDetailsDtos,
            candidatePapers
        );

        if (payload == null)
        {
            return new JobProcessResult
            {
                IsSuccess = false,
                ErrorMessage = Resource.Failed
            };
        }

        var venueDetails = venueDetailsMap.TryGetValue(job.VenueId, out var details)
            ? details
            : (Name: "Unknown Venue", Code: "UnknownCode");

        var createdJob = await _parallelQueryService.ExecuteWriteAsync(async (uow) =>
        {
            var jobid = Guid.NewGuid();

            var payloadMessage = new SyncPayloadMessage
            {
                JobId = jobid,
                VenueCode = venueDetails.Code,
                IsPartialSync = isPartialSync,
                VenueSyncPayload = payload
            };

            var payloadJson = JsonSerializer.Serialize(payloadMessage);

            var fileName = $"payload_{venueDetails.Code}_{jobid}.json";

            var filePath = await _payloadStorageService.WritePayloadAsync(fileName, payloadJson);

            var scheduleName = scheduleDtoLookup.TryGetValue(job.ScheduleId, out var scheduleDto) ? scheduleDto.Name : string.Empty;

            var syncJobEntity = new RealTimeSyncJob
            {
                JobId = jobid,
                BatchId = batchId,
                ScheduleId = job.ScheduleId,
                VenueId = job.VenueId,
                VenueName = venueDetails.Name,
                ScheduleName = scheduleName,
                Status = SyncJobStatus.Pending,
                PayloadFilePath = filePath,
                CandidateCount = payload.Users.Count,
            };

            await uow.Repository<RealTimeSyncJob, long>().AddAsync(syncJobEntity);

            await uow.Complete();

            return syncJobEntity;
        }, audit);

        var message = new SyncScheduleToVenue
        {
            JobId = createdJob.JobId,
            ScheduleId = job.ScheduleId,
            VenueId = job.VenueId,
            PayloadFilePath = createdJob.PayloadFilePath,
            Timestamp = DateTimeHelper.Now
        };

        await _publishEndpoint.Publish(message, context => context.SetRoutingKey(venueDetails.Code), cancellationToken);

        return new JobProcessResult
        {
            IsSuccess = true,
            CreatedJob = createdJob
        };
    }

    private static VenueSyncPayload BuildPayloadForJob(
        SyncJob job,
        Dictionary<long, ScheduleSyncResponseDto> scheduleDtoLookup,
        ILookup<long, PaperSyncResponseDto> paperDtoLookup,
        ILookup<long, PaperFormSyncResponseDto> formDtoLookup,
        ILookup<long, SectionSyncResponseDto> sectionDtoLookup,
        ILookup<long, StageSyncResponseDto> stageDtoLookup,
        ILookup<long, BlockSyncResponseDto> blockDtoLookup,
        ILookup<long, QuestionSyncResponseDto> questionDtoLookup,
        Dictionary<long, DisabilitySyncResponseDto> disabilityDtoLookup,
        ILookup<(long, long), long> candidateLookup,
        Dictionary<long, UserSyncResponseDto> allUserDtos,
        Dictionary<long, CandidateDetailsSyncResponseDto> allCandidateDetailsDtos,
        List<CandidatePaperSyncResponseDto> candidatePapers
    )
    {
        if (!scheduleDtoLookup.TryGetValue(job.ScheduleId, out var scheduleDto))
        {
            return null;
        }

        var payload = new VenueSyncPayload
        {
            Schedule = scheduleDto
        };

        var papersForSchedule = paperDtoLookup[job.ScheduleId].ToList();

        payload.Papers.AddRange(papersForSchedule);

        var uniquePaperTemplateIds = papersForSchedule.Select(p => p.OriginalPaperId).Distinct();

        payload.Forms.AddRange(
            uniquePaperTemplateIds.SelectMany(paperId => formDtoLookup[paperId])
        );

        payload.Sections.AddRange(
            uniquePaperTemplateIds.SelectMany(paperId => sectionDtoLookup[paperId])
        );

        payload.Stages.AddRange(
            payload.Forms.SelectMany(f => stageDtoLookup[f.OriginalPaperFormId])
        );

        payload.Blocks.AddRange(
            payload.Forms.SelectMany(f => blockDtoLookup[f.OriginalPaperFormId])
        );

        payload.Questions.AddRange(
            payload.Forms.SelectMany(form => questionDtoLookup[form.OriginalPaperFormId])
        );

        var candidateIdsForJob = candidateLookup[(job.ScheduleId, job.VenueId)];
        var relevantSchedulePaperIds = new HashSet<long>(papersForSchedule.Select(p => p.OriginalSchedulePaperId));

        payload.CandidatePapers.AddRange(
            candidatePapers
                .Where(a =>
                    candidateIdsForJob.Contains(a.CandidateId) &&
                    relevantSchedulePaperIds.Contains(a.SchedulePaperId) &&
                    a.VenueId == job.VenueId
                )
        );

        var uniqueCandidatesIds = candidateIdsForJob.Distinct();

        foreach (var candidateId in uniqueCandidatesIds)
        {
            if (allUserDtos.TryGetValue(candidateId, out var userDto))
                payload.Users.Add(userDto);

            if (allCandidateDetailsDtos.TryGetValue(candidateId, out var candidateDetailsDto))
            {
                payload.CandidateDetails.Add(candidateDetailsDto);
            }
        }

        if (disabilityDtoLookup?.Count > 0)
        {
            payload.Disabilities.AddRange(disabilityDtoLookup.Values);
        }

        return payload;
    }

    #endregion Job Processing


    #region Question Helper Methods 

    private static QuestionSyncResponseDto MapToDto(
        QuestionBaseInternal q,
        QuestionDetailsInternal d,
        List<Guid> fileIds,
        long? formId,
        double score,
        string status,
        long sectionId,
        long blockId
    )
    {
        var correctChoices = d?.Choices.Where(c => c.IsCorrectAnswer).ToArray() ?? [];

        var modelAnswerTexts = !string.IsNullOrEmpty(d?.ModelAnswer)
             ? [d.ModelAnswer]
             : correctChoices.Select(c => c.ChoiceText).ToArray();

        var choicesDtos = d?.Choices.ConvertAll(c => new QuestionChoiceDto
        {
            Id = c.Id,
            ChoiceText = c.ChoiceText,
            IsCorrectAnswer = c.IsCorrectAnswer,
            AttachmentFileName = c.AttachmentFileName,
            ChoiceOrderId = c.ChoiceOrderId
        }) ?? [];

        var matchingPairQuestionItemsDto = d.MatchingPairQuestionItems?.Select(m => new MatchingPairQuestionItemInternal
        {
            Id = m.Id,
            Body = m.Body,
            ColumnOrder = m.ColumnOrder,
            QuestionDetailsId = m.QuestionDetailsId,
            IsDataSource = m.IsDataSource
        }).ToList() ?? [];

        FileUploadSettingsResponseDto settingsDto = null;

        if (q.FileUploadSettings != null)
        {
            settingsDto = new FileUploadSettingsResponseDto
            {
                ShowAnswerTextArea = q.FileUploadSettings.ShowAnswerTextArea,
                SupportedFileExtensions = q.FileUploadSettings.SupportedFileExtensions,
                UploadedFilesCount = q.FileUploadSettings.UploadedFilesCount,
                SingleFileMaxSizeInMB = q.FileUploadSettings.SingleFileMaxSizeInMB,
                QuestionMetadataId = q.FileUploadSettings.QuestionMetadataId
            };
        }

        return new QuestionSyncResponseDto
        {
            OriginalQuestionId = q.Id,
            PaperFormId = formId ?? 0,
            Code = q.Code,
            Type = q.TypeName,
            IsAutoCorrectable = q.IsAutoCorrectable ?? false,
            Layout = q.LayoutName,
            Subject = q.SubjectName,
            Body = d?.Body ?? string.Empty,
            Instructions = d?.Instructions,
            AttachmentFileName = d?.AttachmentFileName,
            Language = d?.LanguageName ?? string.Empty,
            UseArabicNumbers = d?.UseArabicNumbers ?? false,
            ChoicesShuffled = d?.HasShuffled ?? false,
            ScientificEditorPanelEnabled = q.ScientificEditorPanelEnabled,
            FileManagerEditorPanelEnabled = q.FileManagerEditorPanelEnabled,
            Score = score,
            MaxWords = d?.MaxWords,
            Delta = q.Delta,
            SectionId = sectionId,
            BlockId = blockId != 0 ? blockId : null,
            PaperQuestionStatus = status,
            FileIds = fileIds,
            ModelAnswerTexts = modelAnswerTexts,
            ModelAnswerIds = [.. correctChoices.Select(c => c.Id)],
            Choices = JsonSerializer.Serialize(choicesDtos),
            SegmentQuestionProperties = JsonSerializer.Serialize(d?.SegmentQuestionProperties),
            FileUploadSettings = JsonSerializer.Serialize(settingsDto),
            MatchingPairQuestionItems = JsonSerializer.Serialize(matchingPairQuestionItemsDto),
            MaxRecordingTimeInSeconds = d?.MaxRecordingTimeInSeconds ?? 0,
            SubQuestions = []
        };
    }

    private static QuestionSyncResponseDto CloneSubQuestion(
        QuestionSyncResponseDto template,
        long parentId,
        long formId,
        long sectionId,
        long blockId,
        double score
    )
    {
        return new QuestionSyncResponseDto
        {
            OriginalQuestionId = template.OriginalQuestionId,
            ParentId = parentId,
            IsRoot = false,
            PaperFormId = formId,
            SectionId = sectionId,
            BlockId = blockId != 0 ? blockId : null,
            Score = score,
            Delta = template.Delta,
            MaxWords = template.MaxWords,
            Code = template.Code,
            Type = template.Type,
            IsAutoCorrectable = template.IsAutoCorrectable,
            Layout = template.Layout,
            Subject = template.Subject,
            Body = template.Body,
            Instructions = template.Instructions,
            AttachmentFileName = template.AttachmentFileName,
            Language = template.Language,
            ChoicesShuffled = template.ChoicesShuffled,
            ScientificEditorPanelEnabled = template.ScientificEditorPanelEnabled,
            FileManagerEditorPanelEnabled = template.FileManagerEditorPanelEnabled,
            FileIds = template.FileIds,
            PaperQuestionStatus = template.PaperQuestionStatus,
            ModelAnswerTexts = template.ModelAnswerTexts,
            ModelAnswerIds = template.ModelAnswerIds,
            Choices = template.Choices,
            FileUploadSettings = template.FileUploadSettings,
            MaxRecordingTimeInSeconds = template.MaxRecordingTimeInSeconds,
            UseArabicNumbers = template.UseArabicNumbers,
            SegmentQuestionProperties = template.SegmentQuestionProperties,
            SubQuestions = []
        };
    }

    #endregion


    #region Helper Methods

    private async Task<JobProcessResult> PingVenueAsync(
        long venueId,
        Dictionary<long, (string Name, string Code)> venueDetailsMap,
        CancellationToken cancellationToken
    )
    {
        var result = new JobProcessResult { IsSuccess = false };

        var venueDetails = venueDetailsMap.TryGetValue(venueId, out var details)
            ? details
            : (Name: "Unknown Venue", Code: "UnknownCode");

        try
        {
            var destination = new Uri($"exchange:{SharedConstants.ScheduleExchangeName}?type=topic");

            var client = _bus.CreateRequestClient<IPingVenueConnection>(destination);

            await client.GetResponse<IPongVenueConnection>(
                new { VenueCode = venueDetails.Code },
                callback: x => x.UseExecute(context => context.SetRoutingKey(venueDetails.Code)),
                cancellationToken: cancellationToken,
                timeout: RequestTimeout.After(s: 2)
            );
        }
        catch (RequestTimeoutException)
        {
            result.ErrorMessage = $"{Resource.ConnectionFaildForVenue} ({venueDetails.Code})";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"{Resource.ConnectionFaildForVenue} '{venueDetails.Code}': {ex.Message}";
            return result;
        }

        result.IsSuccess = true;

        return result;
    }

    private async Task<ApiResponse> LockSync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var orgId = _filterParamsValues.OrganizationId;

        var cacheKey = new OrganizationCacheKey { KeyType = GlobalCacheKeys.GlobalSyncLock, OrganizationId = orgId };

        var existingLock = _memoryCacheRepository.GetItemFromCache<GlobalSyncLockDto, OrganizationCacheKey>(cacheKey);

        if (existingLock != null)
        {
            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.Conflict,
                Resource.CurrentSyncProgress,
                null);
        }

        const int lockInMinutes = 3;

        var expirationTime = System.DateTime.UtcNow.AddMinutes(lockInMinutes);

        var lockData = new GlobalSyncLockDto { BatchId = batchId, ExpirationTimeUtc = expirationTime };

        _memoryCacheRepository.SetItemInCache<GlobalSyncLockDto, OrganizationCacheKey>(cacheKey, lockData, TimeSpan.FromMinutes(lockInMinutes));

        await _syncNotificationService.BroadcastGlobalSyncStartedAsync(orgId, expirationTime, cancellationToken);

        return _commonService._apiResponse.GetApiResponse(
            CustomCodeStatus.Success,
            HttpStatusCode.OK
        );
    }

    private async Task ReleaseGlobalLockAsync(CancellationToken cancellationToken = default)
    {
        var orgId = _filterParamsValues.OrganizationId;

        var cacheKey = new OrganizationCacheKey { KeyType = GlobalCacheKeys.GlobalSyncLock, OrganizationId = orgId };

        _memoryCacheRepository.RemoveItemFromCache<OrganizationCacheKey>(cacheKey);

        await _syncNotificationService.BroadcastGlobalSyncFinishedAsync(orgId, cancellationToken);
    }

    #endregion
}
