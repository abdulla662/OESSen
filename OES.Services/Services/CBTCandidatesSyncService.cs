using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OES.Core.Entities;
using OES.Core.Entities.CBTCandidates;
using OES.Core.Entities.CBTCandidates.Views;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.CBTCandidates;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class CBTCandidatesSyncService : ICBTCandidatesSyncService
    {
        private const string AutoSyncUserEmail = "SYSTEM_AUTO_SYNC";
        private int NumberofDaysToSync = 2; // 1: If we need to sync only today; and 2: If we need to sync today and tomorrow; and 7: if we want to sync a week
        private static bool _isSyncRunning = false;

        private bool IsAutoSyncMode() => _filterParamsValues?.UserEmail == AutoSyncUserEmail;
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INotificationHubService _notificationHubService;
        private readonly IConfiguration _configuration;
        private readonly ICandidateService _candidateService;
        private readonly IMemoryCacheRepository _memoryCacheRepository;
        private readonly ILogger<CBTCandidatesSyncService> _logger;

        public CBTCandidatesSyncService(
            ICommonService commonService,
            FilterParamsValues filterParamsValues,
            IHttpClientFactory httpClientFactory,
            INotificationHubService notificationHubService,
            IConfiguration configuration,
            ICandidateService candidateService,
            IMemoryCacheRepository memoryCacheRepository,
            ILogger<CBTCandidatesSyncService> logger
        )
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _httpClientFactory = httpClientFactory;
            _notificationHubService = notificationHubService;
            _configuration = configuration;
            _candidateService = candidateService;
            _memoryCacheRepository = memoryCacheRepository;
            _logger = logger;
        }

        public async Task<ApiResponse> SyncCBTCandidatesByVenueAndDateAsync(string venueCode, DateTime examDate)
        {
            var (validationResult, venue) = await ValidateSyncInputsAsync(venueCode, examDate);
            if (validationResult.CustomCodeStatus != CustomCodeStatus.Success)
                return validationResult;

            var tcIds = ParseTCIds(venue.TCIds);

            var totalNew = 0;
            var totalExisting = 0;
            var countedIds = new HashSet<long>();
            var isAutoSync = IsAutoSyncMode();

            foreach (var centerCode in tcIds)
            {
                var jobStartTime = DateTimeHelper.Now;
                var cbtCandidatesScyncJob = new CBTCandidatesScyncJobs
                {
                    VenueCode = venueCode,
                    CenterCode = centerCode,
                    SyncDate = examDate.Date,
                    JobStartTime = jobStartTime,
                    IsSuccess = true,
                    ErrorCode = 0,
                    IsManualSync = !isAutoSync,
                    IsAutoSync = isAutoSync,
                    CreationUser = _filterParamsValues.UserEmail ?? nameof(System),
                    OrganizationId = _filterParamsValues.OrganizationId,
                    OrganizationSignature = _filterParamsValues.Signature,
                    CreationDate = jobStartTime
                };

                var cbtCandidates = await CallCBTApiAsync(centerCode, examDate, venue.IsPBT);

                if (cbtCandidates == null)
                {
                    await HandleApiCallFailureAsync(cbtCandidatesScyncJob, venueCode, centerCode, examDate, Resource.FailedToRetrieveCandidatesFromCBTAPI);
                    continue;
                }

                if (cbtCandidates.Count == 0)
                {
                    await HandleNoCandidatesFoundAsync(cbtCandidatesScyncJob, venueCode, centerCode, examDate);
                    continue;
                }

                var uncounted = cbtCandidates.Where(c => !countedIds.Contains(c.RegistrationId)).ToList();
                foreach (var c in uncounted)
                    countedIds.Add(c.RegistrationId);

                if (uncounted.Count == 0)
                    continue;

                var filterResult = await FilterCandidatesAsync(uncounted);
                var newCandidates = filterResult.NewCandidates;
                var candidatesToReprocess = filterResult.CandidatesToUpdate;

                if (newCandidates.Count == 0 && candidatesToReprocess.Count == 0)
                {
                    await HandleAllDuplicatesAsync(cbtCandidatesScyncJob, uncounted, venueCode, centerCode, examDate);
                    continue;
                }

                await SaveAndProcessCandidatesAsync(cbtCandidatesScyncJob, newCandidates, candidatesToReprocess, uncounted, venue.Name, venueCode, centerCode, examDate, isAutoSync);
                totalNew += newCandidates.Count + candidatesToReprocess.Count;
                totalExisting += uncounted.Count - (newCandidates.Count + candidatesToReprocess.Count);
            }

            var message = (totalNew, totalExisting) switch
            {
                (0, > 0) => string.Format(Resource.AllCandidatesAlreadyExist, totalExisting),
                ( > 0, 0) => string.Format(Resource.AddedNewCandidates, totalNew),
                ( > 0, > 0) => string.Format(Resource.AddedNewCandidatesExistingSkipped, totalNew, totalExisting),
                _ => Resource.NoCandidatesFound
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, message);
        }

        public async Task<ApiResponse> ExecuteSyncForDaysAsync()
        {
            // Get all active venues
            var venues = await _commonService._unitOfWork.Repository<Venue, long>()
                .Query()
                .AsNoTracking()
                .Where(v => v.IsActive && !v.IsDeleted)
                .ToListAsync();

            if (venues.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoActiveVenuesFoundInTheSystem
                );
            }

            var totalNew = 0;
            var totalExisting = 0;
            var countedIds = new HashSet<long>();

            // Sequential loop to avoid DbContext concurrency issue
            foreach (var venue in venues)
            {
                if (string.IsNullOrWhiteSpace(venue.TCIds))
                {
                    _logger.LogWarning("Venue {VenueCode} has no TCIds configured, skipping.", venue.Code);
                    continue;
                }

                var today = DateTimeHelper.Now.Date;

                for (int i = 0; i < NumberofDaysToSync; i++)
                {
                    var (New, Existing) = await SyncAndCountAsync(venue, today.AddDays(i), countedIds);
                    totalNew += New;
                    totalExisting += Existing;
                }
            }

            var message = (totalNew, totalExisting) switch
            {
                (0, > 0) => string.Format(Resource.AllCandidatesAlreadyExist, totalExisting),
                ( > 0, 0) => string.Format(Resource.AddedNewCandidates, totalNew),
                ( > 0, > 0) => string.Format(Resource.AddedNewCandidatesExistingSkipped, totalNew, totalExisting),
                _ => Resource.NoCandidatesFound
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, message);
        }

        private async Task<(int New, int Existing)> SyncAndCountAsync(Venue venue, DateTime examDate, HashSet<long> countedIds)
        {
            var tcIds = ParseTCIds(venue.TCIds);
            var responses = new List<(int New, int Existing)>();

            foreach (var centerCode in tcIds)
            {
                var isAutoSync = IsAutoSyncMode();
                var job = new CBTCandidatesScyncJobs
                {
                    VenueCode = venue.Code,
                    CenterCode = centerCode,
                    SyncDate = examDate.Date,
                    JobStartTime = DateTimeHelper.Now,
                    IsSuccess = true,
                    ErrorCode = 0,
                    IsManualSync = !isAutoSync,
                    IsAutoSync = isAutoSync,
                    CreationUser = _filterParamsValues.UserEmail ?? MiscConstants.DefaultSystemUser,
                    OrganizationId = _filterParamsValues.OrganizationId,
                    OrganizationSignature = _filterParamsValues.Signature,
                    CreationDate = DateTimeHelper.Now
                };

                var candidates = await CallCBTApiAsync(centerCode, examDate, venue.IsPBT);
                if (candidates == null)
                {
                    await HandleApiCallFailureAsync(job, venue.Code, centerCode, examDate, Resource.FailedToRetrieveCandidatesFromCBTAPI);
                    continue;
                }

                if (candidates.Count == 0)
                {
                    await HandleNoCandidatesFoundAsync(job, venue.Code, centerCode, examDate);
                    continue;
                }

                var uncounted = candidates.Where(c => !countedIds.Contains(c.RegistrationId)).ToList();
                foreach (var c in uncounted)
                    countedIds.Add(c.RegistrationId);

                if (uncounted.Count == 0)
                    continue;

                var filterResult = await FilterCandidatesAsync(uncounted);
                var newCandidates = filterResult.NewCandidates;
                var candidatesToReprocess = filterResult.CandidatesToUpdate;
                var processedCount = newCandidates.Count + candidatesToReprocess.Count;
                var existingCount = uncounted.Count - processedCount;

                if (processedCount > 0)
                {
                    await SaveAndProcessCandidatesAsync(job, newCandidates, candidatesToReprocess, uncounted, venue.Name, venue.Code, centerCode, examDate, isAutoSync);
                    responses.Add((processedCount, existingCount));
                }
            }

            return (responses.Sum(r => r.New), responses.Sum(r => r.Existing));
        }

        public async Task<ApiResponse> GetSyncJobDetailsAsync(long jobId)
        {
            // Validate jobId
            if (jobId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.JobIdMustBeGreaterThanZero
                );
            }

            var job = await _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>()
                .GetObjAsync(j => j.Id == jobId);

            if (job is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.SyncJobNotFound
                );
            }

            var candidatesCount = await _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>()
                .Query()
                .AsNoTracking()
                .Where(c => c.JobId == jobId)
                .CountAsync();

            var jobDetails = new
            {
                job.Id,
                job.VenueCode,
                job.SyncDate,
                job.JobStartTime,
                job.JobEndTime,
                DurationSeconds = job.JobEndTime.HasValue ? (job.JobEndTime.Value - job.JobStartTime).TotalSeconds : 0,
                job.IsSuccess,
                job.ErrorCode,
                job.ErrorMessage,
                job.TotalCandidates,
                CandidatesInDatabase = candidatesCount,
                job.Request
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.JobDetailsRetrievedSuccessfully,
                jobDetails
            );
        }

        public async Task<ApiResponse> GetReceivedCandidatesByJobIdAsync(long jobId, PaginationSearchModel pagination)
        {
            // Validate jobId
            if (jobId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.JobIdMustBeGreaterThanZero
                );
            }

            if (pagination == null)
                pagination = new PaginationSearchModel { PageIndex = 0, PageSize = 10 };

            var query = _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>()
                .Query()
                .AsNoTracking()
                .Where(c => c.JobId == jobId);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
            {
                string searchKey = pagination.SearchKey.ToLower();

                query = query.Where(c =>
                    c.NationalId.ToLower().Contains(searchKey) ||
                    c.FirstName.ToLower().Contains(searchKey) ||
                    c.LastName.ToLower().Contains(searchKey) ||
                    c.FirstEnglishName.ToLower().Contains(searchKey) ||
                    c.LastEnglishName.ToLower().Contains(searchKey)
                );
            }

            // Apply date filters
            if (pagination.FromDate.HasValue)
                query = query.Where(c => c.CreationDate >= pagination.FromDate.Value);

            if (pagination.ToDate.HasValue)
                query = query.Where(c => c.CreationDate <= pagination.ToDate.Value);

            // Sorting
            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(c => c.CreationDate)
                : query.OrderBy(c => c.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesFoundForThisJob
                );
            }

            // Pagination
            if (!pagination.PaginationOff)
            {
                int pageSize = pagination.PageSize > 0 ? pagination.PageSize : 10;
                query = query.Skip(pagination.PageIndex * pageSize).Take(pageSize);
            }

            var candidates = await query.ToListAsync();

            var tableData = new CustomTableData<CBTCandidatesRecievedData>(candidates, totalRecords);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesRetrievedSuccessfully,
                tableData
            );
        }

        private async Task<(ApiResponse Response, Venue Venue)> ValidateSyncInputsAsync(string venueCode, DateTime examDate)
        {
            ApiResponse Fail(CustomCodeStatus code, HttpStatusCode http, string msg) =>
                _commonService._apiResponse.GetApiResponse(code, http, msg);

            if (string.IsNullOrWhiteSpace(venueCode))
                return (Fail(CustomCodeStatus.ValidationError, HttpStatusCode.BadRequest, Resource.VenueCodeMustNotBeEmpty), null);

            if (examDate.Date < DateTimeHelper.Now.Date)
                return (Fail(CustomCodeStatus.ValidationError, HttpStatusCode.BadRequest, Resource.ExamDateCannotBeInThePast), null);

            var venue = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .GetObjAsync(v => v.Code == venueCode && v.IsActive && !v.IsDeleted);

            if (venue is null)
                return (Fail(CustomCodeStatus.ValidationError, HttpStatusCode.BadRequest, Resource.VenueNotFoundOrNotActive), null);

            if (string.IsNullOrWhiteSpace(venue.TCIds) || ParseTCIds(venue.TCIds).Count == 0)
                return (Fail(CustomCodeStatus.ValidationError, HttpStatusCode.BadRequest, Resource.TCIdsMustNotBeEmpty), null);

            return (_commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK), venue);
        }

        private async Task<List<CBTCandidateResponseDto>> CallCBTApiAsync(string centerCode, DateTime examDate, bool isPbt)
        {
            var cbtSettings = _configuration.GetSection(MiscConstants.CBTApiSettingsSection).Get<CBTApiSettings>();

            var formattedDate = examDate.ToString("dd-MM-yyyy");
            var apiUrl = $"{cbtSettings.BaseUrl}{cbtSettings.Endpoint}?centerID={centerCode}&ExamDate={formattedDate}&IsPBT={isPbt}";
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(cbtSettings.TimeoutSeconds);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            for (int attempt = 1; attempt <= cbtSettings.RetryAttempts; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                    request.Headers.Add("PrivateKey", cbtSettings.PrivateKey);

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonSerializer.Deserialize<CBTApiResponseDto>(json, options);
                        return apiResponse?.ResponseDetails ?? [];
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _logger.LogError("CBT API auth failure for centerCode={CenterCode}: {StatusCode}", centerCode, response.StatusCode);
                        return null;
                    }

                    _logger.LogWarning("CBT API attempt {Attempt}/{Max} failed for centerCode={CenterCode}: {StatusCode}", attempt, cbtSettings.RetryAttempts, centerCode, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CBT API attempt {Attempt}/{Max} threw exception for centerCode={CenterCode}", attempt, cbtSettings.RetryAttempts, centerCode);
                }

                if (attempt < cbtSettings.RetryAttempts)
                {
                    const int maxBackoffSeconds = 60;
                    var delaySeconds = Math.Min(Math.Pow(2, attempt), maxBackoffSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            _logger.LogError("CBT API all {Max} attempts exhausted for centerCode={CenterCode}", cbtSettings.RetryAttempts, centerCode);
            return null;
        }

        private async Task<(List<CBTCandidateResponseDto> NewCandidates, List<CBTCandidateResponseDto> CandidatesToUpdate)> FilterCandidatesAsync(List<CBTCandidateResponseDto> cbtCandidates)
        {
            if (cbtCandidates.Count == 0)
                return (new List<CBTCandidateResponseDto>(), new List<CBTCandidateResponseDto>());

            var registrationIds = cbtCandidates.ConvertAll(x => x.RegistrationId);
            var nationalIds = cbtCandidates.ConvertAll(x => x.NationalId).Distinct().ToList();

            var existingReceivedData = await _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>()
                .Query()
                .AsNoTracking()
                .Where(c => registrationIds.Contains(c.RegistrationId))
                .ToListAsync();

            var existingReceivedSet = new HashSet<long>(existingReceivedData.Select(c => c.RegistrationId));

            var newCandidates = cbtCandidates.Where(c => !existingReceivedSet.Contains(c.RegistrationId)).ToList();

            var existingCbtCandidates = cbtCandidates.Where(c => existingReceivedSet.Contains(c.RegistrationId)).ToList();

            if (existingCbtCandidates.Count == 0)
                return (newCandidates, new List<CBTCandidateResponseDto>());

            var existingCandidates = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .Query()
                .AsNoTracking()
                .Where(c => nationalIds.Contains(c.NationalId))
                .Select(c => new { c.NationalId, c.Name })
                .ToListAsync();

            var existingSchedulePaperRegNumbers = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query()
                .AsNoTracking()
                .Where(c => registrationIds.Contains(c.RegistrationNumber))
                .Select(c => c.RegistrationNumber)
                .ToListAsync();

            var existingCandidateDict = existingCandidates
                .GroupBy(c => c.NationalId)
                .ToDictionary(g => g.Key, g => g.First().Name);
            var existingSchedulePaperSet = new HashSet<long>(existingSchedulePaperRegNumbers);

            var candidatesToUpdate = existingCbtCandidates
                .Where(c =>
                    !existingCandidateDict.ContainsKey(c.NationalId) ||
                    !existingSchedulePaperSet.Contains(c.RegistrationId) ||
                    (existingCandidateDict.TryGetValue(c.NationalId, out var existingName) && !string.Equals(existingName?.Trim(), GetFullNameFromDto(c)?.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return (newCandidates, candidatesToUpdate);
        }

        private static string GetFullNameFromDto(CBTCandidateResponseDto dto)
        {
            var arabic = string
                .Join(" ", new[] { dto.FirstName, dto.SecondName, dto.ThirdName, dto.LastName }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            var english = string
                .Join(" ", new[] { dto.FirstEnglishName, dto.MiddleEnglishName, dto.ThirdEnglishName, dto.LastEnglishName }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
        }

        private async Task<ApiResponse> HandleApiCallFailureAsync(
            CBTCandidatesScyncJobs job,
            string venueCode,
            string centerCode,
            DateTime examDate,
            string errorMessage
        )
        {
            job.JobEndTime = DateTimeHelper.Now;
            job.IsSuccess = false;
            job.ErrorCode = (int)HttpStatusCode.InternalServerError;
            job.ErrorMessage = errorMessage;
            job.Request = $"Venue: {venueCode}, Center: {centerCode}, Date: {examDate:dd-MM-yyyy}";
            job.TotalCandidates = 0;

            await _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>().AddAsync(job);

            await _commonService._unitOfWork.Complete();

            _logger.LogError("CBT sync failed for venue={VenueCode} center={CenterCode} date={ExamDate}: {Error}", venueCode, centerCode, examDate, errorMessage);

            //await SendSyncErrorNotificationAsync(venueCode, centerCode, examDate, errorMessage);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                string.Format(Resource.ErrorSyncingCandidates, errorMessage)
            );
        }

        private async Task<ApiResponse> HandleNoCandidatesFoundAsync(
            CBTCandidatesScyncJobs job,
            string venueCode,
            string centerCode,
            DateTime examDate
        )
        {
            job.JobEndTime = DateTimeHelper.Now;
            job.TotalCandidates = 0;
            job.Request = $"Venue: {venueCode}, Center: {centerCode}, Date: {examDate:dd-MM-yyyy}";
            job.IsSuccess = true;

            await _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>().AddAsync(job);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.NoCandidatesFoundFromCBTAPIForVenueAndDate
            );
        }

        private async Task<ApiResponse> HandleAllDuplicatesAsync(
            CBTCandidatesScyncJobs job,
            List<CBTCandidateResponseDto> cbtCandidates,
            string venueCode,
            string centerCode,
            DateTime examDate
        )
        {
            job.JobEndTime = DateTimeHelper.Now;
            job.TotalCandidates = 0;
            job.Request = $"Venue: {venueCode}, Center: {centerCode}, Date: {examDate:dd-MM-yyyy}, All candidates are duplicates";
            job.IsSuccess = true;

            await _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>().AddAsync(job);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.AllCandidatesAlreadyExistInSystem, cbtCandidates.Count)
            );
        }

        private async Task<ApiResponse> SaveAndProcessCandidatesAsync(
            CBTCandidatesScyncJobs job,
            List<CBTCandidateResponseDto> newCandidates,
            List<CBTCandidateResponseDto> candidatesToUpdate,
            List<CBTCandidateResponseDto> allCbtCandidates,
            string venueName,
            string venueCode,
            string centerCode,
            DateTime examDate,
            bool isAutoSync
        )
        {
            var receivedCandidatesToSave = newCandidates.ConvertAll(c => new CBTCandidatesRecievedData
            {
                NationalId = c.NationalId,
                FirstName = c.FirstName,
                SecondName = c.SecondName,
                ThirdName = c.ThirdName,
                LastName = c.LastName,
                FirstEnglishName = c.FirstEnglishName,
                MiddleEnglishName = c.MiddleEnglishName,
                ThirdEnglishName = c.ThirdEnglishName,
                LastEnglishName = c.LastEnglishName,
                CenterArabicName = c.CenterArabicName,
                CenterEnglishName = c.CenterEnglishName,
                VenueCode = venueCode,
                CenterCode = c.CenterCode,
                CityArabicName = c.CityArabicName,
                CityEnglishName = c.CityEnglishName,
                RegionArabicName = c.RegionArabicName,
                RegionEnglishName = c.RegionEnglishName,
                ExamArabicName = c.ExamArabicName,
                ExamEnglishName = c.ExamEnglishName,
                ExamDate = c.ExamDate,
                ExamTypeArabicName = c.ExamTypeArabicName,
                ExamTypeEnglishName = c.ExamTypeEnglishName,
                RegistrationId = c.RegistrationId,
                ShowStatus = c.ShowStatus,
                ExamSeriesCode = c.ExamSeriesCode,
                Status = CBTCandidateStatus.Received,
                IsManualSync = !isAutoSync,
                IsAutoSync = isAutoSync,
                CreationUser = _filterParamsValues.UserEmail ?? MiscConstants.DefaultSystemUser,
                OrganizationId = _filterParamsValues.OrganizationId,
                OrganizationSignature = _filterParamsValues.Signature,
                CreationDate = DateTimeHelper.Now
            });

            // Save job record first
            await _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>().AddAsync(job);
            await _commonService._unitOfWork.Complete();

            // Set JobId for received candidates
            if (receivedCandidatesToSave.Count > 0)
            {
                foreach (var candidate in receivedCandidatesToSave)
                {
                    candidate.JobId = job.Id;
                }

                // Save received candidates to CBTCandidatesRecievedData table
                await _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>().AddRangeAsync(receivedCandidatesToSave);
                await _commonService._unitOfWork.Complete();
            }

            var updatedReceivedData = new List<CBTCandidatesRecievedData>();
            if (candidatesToUpdate.Count > 0)
            {
                var regIds = candidatesToUpdate.ConvertAll(c => c.RegistrationId);

                var existingReceived = await _commonService
                    ._unitOfWork
                    .Repository<CBTCandidatesRecievedData, long>()
                    .Query()
                    .Where(c => regIds.Contains(c.RegistrationId))
                    .ToListAsync();

                updatedReceivedData = [.. existingReceived
                    .Join(candidatesToUpdate, e => e.RegistrationId, n => n.RegistrationId, (e, n) =>
                    {
                        e.FirstName = n.FirstName; e.SecondName = n.SecondName; e.ThirdName = n.ThirdName; e.LastName = n.LastName;
                        e.FirstEnglishName = n.FirstEnglishName; e.MiddleEnglishName = n.MiddleEnglishName;
                        e.ThirdEnglishName = n.ThirdEnglishName; e.LastEnglishName = n.LastEnglishName;
                        e.CenterArabicName = n.CenterArabicName; e.CenterEnglishName = n.CenterEnglishName;
                        e.CenterCode = n.CenterCode; e.VenueCode = venueCode;
                        e.CityArabicName = n.CityArabicName; e.CityEnglishName = n.CityEnglishName;
                        e.RegionArabicName = n.RegionArabicName; e.RegionEnglishName = n.RegionEnglishName;
                        e.ExamArabicName = n.ExamArabicName; e.ExamEnglishName = n.ExamEnglishName; e.ExamDate = n.ExamDate;
                        e.ExamTypeArabicName = n.ExamTypeArabicName; e.ExamTypeEnglishName = n.ExamTypeEnglishName;
                        e.ExamSeriesCode = n.ExamSeriesCode; e.ShowStatus = n.ShowStatus;
                        e.Status = CBTCandidateStatus.Updated;
                        e.ModeficationDate = DateTimeHelper.Now;
                        e.ModeficationUser = _filterParamsValues.UserEmail ?? MiscConstants.DefaultSystemUser;
                        return e;
                    })];

                if (updatedReceivedData.Count > 0)
                {
                    _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>().UpdateRange(updatedReceivedData);
                    await _commonService._unitOfWork.Complete();
                }
            }

            var allCandidatesToProcess = new List<CBTCandidatesRecievedData>(receivedCandidatesToSave);
            allCandidatesToProcess.AddRange(updatedReceivedData);

            // Process using ProcessDumpCandidateImportWithSchedulePaperAsync for PaperFormId distribution
            var result = await ProcessCandidatesViaImportServiceAsync(allCandidatesToProcess, venueCode, examDate);

            var missingCodesInfo = result.MissingCodes.Count > 0 ? $", MissingPaperCodes: [{string.Join(", ", result.MissingCodes)}]" : "";

            job.JobEndTime = DateTimeHelper.Now;
            job.TotalCandidates = allCandidatesToProcess.Count;
            job.Request = $"Venue: {venueCode}, centerCode: {centerCode}, Date: {examDate:dd-MM-yyyy}, New: {receivedCandidatesToSave.Count}, Updated: {candidatesToUpdate.Count}, Queued: {result.QueuedCount}, Skipped: {result.SkippedCount}{missingCodesInfo}";
            job.IsSuccess = result.Success;
            job.ErrorMessage = result.ErrorMessage;

            _commonService._unitOfWork.Repository<CBTCandidatesScyncJobs, long>().Update(job);
            await _commonService._unitOfWork.Complete();

            if (!result.Success)
            {
                //await SendSyncErrorNotificationAsync(venueName, centerCode, examDate, result.ErrorMessage ?? Resource.FailedToQueueCandidates);
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    string.Format(Resource.ErrorSyncingCandidates, result.ErrorMessage)
                );
            }

            //await SendSyncSuccessNotificationAsync(venueName, centerCode, examDate, allCandidatesToProcess.Count, allCbtCandidates.Count - allCandidatesToProcess.Count);

            var responseMessage = string.Format(Resource.SuccessfullySyncedCandidates, allCandidatesToProcess.Count, result.QueuedCount, result.SkippedCount);
            if (result.MissingCodes.Count > 0)
            {
                responseMessage += string.Format(Resource.WarningNoMatchingSchedulePaper, string.Join(", ", result.MissingCodes));
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                responseMessage
            );
        }

        private async Task<(bool Success, string ErrorMessage, int QueuedCount, int SkippedCount, List<string> MissingCodes)> ProcessCandidatesViaImportServiceAsync(
            List<CBTCandidatesRecievedData> receivedCandidates,
            string venueCode,
            DateTime examDate
        )
        {
            var missingCodes = new List<string>();
            int totalQueued = 0;
            int totalSkipped = 0;

            // Get VenueId from VenueCode
            var venue = await _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .AsNoTracking()
                .Where(v => v.Code == venueCode && !v.IsDeleted && v.IsActive)
                .Select(v => new { v.Id })
                .FirstOrDefaultAsync();

            if (venue == null)
                return (false, string.Format(Resource.VenueNotFoundWithCode, venueCode), 0, receivedCandidates.Count, missingCodes);

            // Group candidates by ExamSeriesCode and ExamDate
            var groupedCandidates = receivedCandidates
                .Where(c => !string.IsNullOrWhiteSpace(c.ExamSeriesCode))
                .GroupBy(c => new { c.ExamSeriesCode, ExamDateOnly = DateOnly.FromDateTime(c.ExamDate) })
                .ToList();

            if (groupedCandidates.Count == 0)
                return (true, Resource.NoExamSeriesCodesFound, 0, receivedCandidates.Count, missingCodes);

            var examSeriesCodes = groupedCandidates.ConvertAll(g => ExtractPaperCodeWithoutGender(g.Key.ExamSeriesCode)).Distinct().ToList();

            // Get SchedulePapers linked to this venue (date check done in matching below using candidate's ExamDate)
            var schedulePapers = await _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .AsNoTracking()
                .Where(sp => sp.PaperMetadata != null &&
                             !sp.IsDeleted &&
                             examSeriesCodes.Contains(sp.PaperMetadata.Code) &&
                             sp.ScheduleMetadata.Venues.Any(sv => sv.VenueId == venue.Id && !sv.IsDeleted && sv.IsActive))
                .Select(sp => new { sp.Id, Code = sp.PaperMetadata.Code, sp.StartDate, sp.EndDate })
                .ToListAsync();

            var matchedCodes = schedulePapers.ConvertAll(sp => sp.Code).Distinct().ToList();
            missingCodes = [.. examSeriesCodes.Except(matchedCodes)];

            if (schedulePapers.Count == 0)
                return (true, Resource.NoMatchingSchedulePapersFound, 0, receivedCandidates.Count, missingCodes);

            // Match groups to papers using candidate's ExamDate for date range check
            var matchedGroups = groupedCandidates
                .Select(group => new
                {
                    ExamSeriesCode = group.Key.ExamSeriesCode,
                    Candidates = group.ToList(),
                    MatchingPaper = schedulePapers.FirstOrDefault(sp =>
                        sp.Code == ExtractPaperCodeWithoutGender(group.Key.ExamSeriesCode) &&
                        sp.StartDate <= group.Key.ExamDateOnly &&
                        sp.EndDate >= group.Key.ExamDateOnly)
                })
                .Where(g => g.MatchingPaper != null)
                .ToList();

            totalSkipped = groupedCandidates.Sum(g => g.Count()) - matchedGroups.Sum(g => g.Candidates.Count);

            if (matchedGroups.Count == 0)
                return (true, Resource.NoMatchingGroupsFound, 0, totalSkipped, missingCodes);

            // Create all batch histories at once
            var batchHistories = matchedGroups.ConvertAll(g => new CandidateBatchImportHistory
            {
                Name = $"CBT Sync - {g.ExamSeriesCode} - {examDate:yyyy-MM-dd}",
                FileId = Guid.NewGuid(),
                SchedulePaperId = g.MatchingPaper.Id,
                IsReversed = false,
                DataSource = DataSource.CBT,
                CreationDate = DateTimeHelper.Now,
                CreationUser = _filterParamsValues.UserEmail ?? "SYSTEM",
                OrganizationId = _filterParamsValues.OrganizationId,
                OrganizationSignature = _filterParamsValues.Signature
            });

            // Save all batches in one database call
            await _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>().AddRangeAsync(batchHistories);
            await _commonService._unitOfWork.Complete();

            // Get all NationalIds from matched candidates to check existence in Candidate table
            var allMatchedCandidates = matchedGroups.SelectMany(g => g.Candidates).ToList();
            var nationalIds = allMatchedCandidates.ConvertAll(c => c.NationalId).Distinct().ToList();

            // Query existing candidates from Candidate table
            var existingCandidates = await _commonService._unitOfWork
                .Repository<Candidate, long>()
                .Query()
                .AsNoTracking()
                .Where(c => nationalIds.Contains(c.NationalId))
                .Select(c => new { c.NationalId, c.Name })
                .ToListAsync();

            // Collect all candidates that will be assigned to paper
            var assignedCandidates = new List<CBTCandidatesRecievedData>();

            // Process each group with its matching SchedulePaper and saved batch using LINQ
            totalQueued = matchedGroups
                .Zip(batchHistories, (group, batch) => new { Group = group, Batch = batch })
                .Sum(pair =>
                {
                    // Mark candidates as assigned to paper and set Status based on existence
                    pair.Group.Candidates.ForEach(c =>
                    {
                        c.IsAssignedToPaper = true;
                        var existing = existingCandidates.FirstOrDefault(e => e.NationalId == c.NationalId);
                        var currentName = GetFullName(c);

                        c.Status = existing == null
                            ? CBTCandidateStatus.Added
                            : !string.Equals(existing.Name?.Trim(), currentName?.Trim(), StringComparison.OrdinalIgnoreCase) ? CBTCandidateStatus.Updated : CBTCandidateStatus.Exist;
                    });

                    assignedCandidates.AddRange(pair.Group.Candidates);

                    // Convert to DumpImportCandidateRequestDto format
                    var candidatesForImport = pair.Group.Candidates.ConvertAll(c => new DumpImportCandidateRequestDto
                    {
                        CandidateCode = c.RegistrationId.ToString(),
                        Name = GetFullName(c),
                        NationalId = c.NationalId,
                        UserName = c.NationalId,
                        Password = c.NationalId,
                        Qualification = "",
                        DateOfBirth = DateTime.MinValue.ToString(),
                        Address = $"{c.CityArabicName}, {c.RegionArabicName}".Trim(',', ' '),
                        Mobile = "",
                        Email = string.Format(MiscConstants.EmailDomain, c.NationalId),
                        PhotoURL = ExtractGender(c.ExamSeriesCode) == 0 ? "male-img.jpg" : "female-img.jpg",
                        SignatureURL = "signature-img.png",
                        Gender = ExtractGender(c.ExamSeriesCode), // Extract gender from code
                        RegistrationCenterCode = c.CenterCode,
                        RegistrationDateTime = DateTimeHelper.Now.ToString(),
                        RegistrationNumber = c.RegistrationId.ToString(),
                        VenueCode = venueCode,
                        CandidateExamDate = c.ExamDate.ToString(),
                    });

                    // Store in memory cache
                    var candidatesKey = Guid.NewGuid();
                    _memoryCacheRepository.SetItemInCache(candidatesKey, candidatesForImport.ConvertAll(c => (AddMultipleCandidateDto)c));

                    FilterParamsValues filterParamsValues = new FilterParamsValues
                    {
                        OrganizationId = _filterParamsValues.OrganizationId,
                        Signature = _filterParamsValues.Signature,
                        UserId = _filterParamsValues.UserId,
                        UserEmail = _filterParamsValues.UserEmail,
                    };

                    // Queue for processing via Hangfire
                    BackgroundJob.Enqueue(() => _candidateService.ProcessCandidateImportToSchedulePaperAsync(
                        candidatesKey,
                        pair.Group.MatchingPaper.Id,
                        venue.Id,
                        pair.Batch.Id,
                        filterParamsValues
                    ));

                    return pair.Group.Candidates.Count;
                });

            var changedCandidates = assignedCandidates.Where(c => c.Status == CBTCandidateStatus.Updated).ToList();
            if (changedCandidates.Count > 0)
            {
                var natIds = changedCandidates.ConvertAll(c => c.NationalId);

                var candidatesToUpdate = await _commonService._unitOfWork
                    .Repository<Candidate, long>()
                    .Query()
                    .Where(c => natIds.Contains(c.NationalId))
                    .ToListAsync();

                foreach (var candidate in candidatesToUpdate)
                {
                    var changed = changedCandidates.FirstOrDefault(ch => ch.NationalId == candidate.NationalId);
                    if (changed != null)
                    {
                        candidate.Name = GetFullName(changed);
                        candidate.IsSynced = false;
                    }
                }

                _commonService._unitOfWork.Repository<Candidate, long>().UpdateRange(candidatesToUpdate);

                await _commonService._unitOfWork.Complete();
            }

            // Update IsAssignedToPaper in database for all assigned candidates
            if (assignedCandidates.Count > 0)
            {
                _commonService._unitOfWork.Repository<CBTCandidatesRecievedData, long>().UpdateRange(assignedCandidates);
                await _commonService._unitOfWork.Complete();
            }

            var success = totalQueued > 0 || (totalSkipped == 0 && missingCodes.Count == 0);

            return (success, null, totalQueued, totalSkipped, missingCodes);
        }

        private static string GetFullName(CBTCandidatesRecievedData received)
        {
            var arabic = string.Join(" ", new[] { received.FirstName, received.SecondName, received.ThirdName, received.LastName }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            var english = string.Join(" ", new[] { received.FirstEnglishName, received.MiddleEnglishName, received.ThirdEnglishName, received.LastEnglishName }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
        }

        private async Task SendSyncSuccessNotificationAsync(string venueName, string centerCode, DateTime examDate, int savedCount, int skippedCount)
        {
            var notificationDto = new NotificationDto(
                entity: NotificationEntity.Candidate,
                operation: NotificationOperation.Imported,
                from: Resource.CBTSyncService,
                status: NotificationStatus.Success,
                type: NotificationTypeStatus.Success,
                parameterName: string.Format(Resource.SuccessfullySyncedCandidatesNotification, savedCount, $"{venueName}:{centerCode}", examDate.ToString("dd-MM-yyyy"), skippedCount)
            );

            if (Guid.TryParse(_filterParamsValues.UserId, out var userId))
            {
                await _notificationHubService.NotifyAsync(notificationDto, userId);
            }
        }

        private async Task SendSyncErrorNotificationAsync(string venueName, string centerCode, DateTime examDate, string errorMessage)
        {
            var notificationDto = new NotificationDto(
                entity: NotificationEntity.Candidate,
                operation: NotificationOperation.Imported,
                from: Resource.CBTSyncService,
                status: NotificationStatus.Failed,
                type: NotificationTypeStatus.Error,
                parameterName: string.Format(Resource.ErrorSyncingCandidatesNotification, $"{venueName}:{centerCode}", examDate.ToString("dd-MM-yyyy"), errorMessage)
            );

            if (Guid.TryParse(_filterParamsValues.UserId, out var userId))
            {
                await _notificationHubService.NotifyAsync(notificationDto, userId);
            }
        }

        public async Task<ApiResponse> GetCBTSyncStatusAsync()
        {
            var status = await _commonService._unitOfWork
                .Repository<CBTSyncStatusView, long>()
                .Query()
                .AsNoTracking()
                .Select(s => new CBTSyncStatusResponseDto
                {
                    TotalCandidates = s.TotalCandidates,
                    ImportedCount = s.ImportedCount,
                    AssignedToPaperCount = s.AssignedToPaperCount,
                    NotAssignedToPaperCount = s.NotAssignedToPaperCount,
                    LastRunTime = s.LastRunTime,
                    IsManualSync = s.IsManualSync,
                    IsAutoSync = s.IsAutoSync,
                    VenueId = s.VenueId,
                    VenueCode = s.VenueCode,
                    VenueName = s.VenueName,
                    TotalVenues = s.TotalVenues,
                    AvgTimeMs = s.AvgTimeMs
                })
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CBTSyncStatusRetrievedSuccessfully,
                status
            );
        }

        public async Task<ApiResponse> GetCentersSyncStatusAsync(DateTime? syncDate = null)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CentersSyncStatusView, long>()
                .Query()
                .AsNoTracking();

            if (syncDate.HasValue)
            {
                query = query.Where(s => s.SyncDate == syncDate.Value.Date);
            }

            var status = await query
                .GroupBy(s => new { s.SyncDate, s.CenterCode, s.CenterName, s.Region, s.VenueId })
                .Select(g => new CentersSyncStatusResponseDto
                {
                    CenterCode = g.Key.CenterCode,
                    CenterName = g.Key.CenterName,
                    Region = g.Key.Region,
                    CBTReceived = g.Sum(x => x.CBTReceived ?? 0),
                    Allocated = g.Sum(x => x.Allocated ?? 0),
                    Pushed = g.Sum(x => x.Pushed ?? 0),
                    Acknowledged = g.Sum(x => x.Acknowledged ?? 0),
                    Status = g.Max(x => x.Status) ?? SyncJobStatus.Pending,
                    LastSync = g.Max(x => x.LastSync),
                    SyncDate = g.Key.SyncDate,
                    VenueId = g.Key.VenueId
                }).ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CentersSyncStatusRetrievedSuccessfully,
                status
            );
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteSyncAutomaticallyAsync(Guid? callerUserId = null)
        {
            // Get organization info from the first active venue for auto sync
            var venueOrgInfo = await _commonService._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .AsNoTracking()
                .Where(v => v.IsActive && !v.IsDeleted)
                .Select(v => new { v.OrganizationId, v.OrganizationSignature })
                .FirstOrDefaultAsync();

            var isManual = callerUserId.HasValue;
            var jobStartTime = DateTimeHelper.Now;

            _filterParamsValues.UserEmail = isManual ? callerUserId.ToString() : AutoSyncUserEmail;
            _filterParamsValues.UserId = isManual ? callerUserId.ToString() : Guid.Empty.ToString();
            _filterParamsValues.OrganizationId = venueOrgInfo?.OrganizationId ?? 1;
            _filterParamsValues.Signature = venueOrgInfo?.OrganizationSignature ?? "AUTO_SYNC";
            _filterParamsValues.Authorize = true;
            _filterParamsValues.ApplyFilter = false;
            _filterParamsValues.ApplyOrganizationIdFilter = false;
            _filterParamsValues.ApplySignatureFilter = false;

            var isSuccess = true;
            var errorMsg = string.Empty;
            ApiResponse? syncResult = null;

            try
            {
                syncResult = await ExecuteSyncForDaysAsync();
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMsg = ex.Message;
                _logger.LogError(ex, "Error during sync");
            }
            finally
            {
                _isSyncRunning = false;

                if (isManual && callerUserId.HasValue)
                {
                    var userId = callerUserId.Value;
                    var duration = (DateTimeHelper.Now - jobStartTime).TotalSeconds;
                    var parameterName = isSuccess
                         ? $"{Resource.ManualSyncCompleted} | {Resource.StartTime}: {jobStartTime:HH:mm:ss} | {Resource.EndTime}: {DateTimeHelper.Now:HH:mm:ss} | {Resource.Duration}: {duration:F1}s | {syncResult?.Message ?? ""}"
                         : $"{Resource.ManualSyncFailed} | {Resource.Error}: {errorMsg} | {Resource.Duration}: {duration:F1}s";

                    var notification = new Notification
                    {
                        Entity = NotificationEntity.Candidate,
                        Operation = NotificationOperation.Imported,
                        Status = isSuccess ? NotificationStatus.Success : NotificationStatus.Failed,
                        Type = isSuccess ? NotificationTypeStatus.Success : NotificationTypeStatus.Error,
                        From = Resource.CBTSyncService,
                        ParameterName = parameterName,
                        CreationDate = DateTimeHelper.Now
                    };

                    notification.AppUserProfileNotifications.Add(new NotificationAppUserProfile
                    {
                        AppUserProfileId = userId,
                        IsRead = false
                    });

                    await _commonService._unitOfWork.Repository<Notification, long>().AddAsync(notification);
                    await _commonService._unitOfWork.Complete();

                    await _notificationHubService.NotifyAsync(new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.CBTSyncService,
                        status: isSuccess ? NotificationStatus.Success : NotificationStatus.Failed,
                        type: isSuccess ? NotificationTypeStatus.Success : NotificationTypeStatus.Error,
                        parameterName: parameterName
                    ), userId);
                }
            }
        }

        private static List<string> ParseTCIds(string tcIds) =>
            [.. tcIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(id => id.Trim())
                 .Where(id => !string.IsNullOrEmpty(id))
            ];

        private static string ExtractPaperCodeWithoutGender(string examSeriesCode)
        {
            if (string.IsNullOrWhiteSpace(examSeriesCode))
                return examSeriesCode;

            if (examSeriesCode.EndsWith("-M", StringComparison.OrdinalIgnoreCase) ||
                examSeriesCode.EndsWith("-F", StringComparison.OrdinalIgnoreCase))
            {
                return examSeriesCode.Substring(0, examSeriesCode.Length - 2);
            }

            return examSeriesCode;
        }

        private static int ExtractGender(string examSeriesCode)
        {
            if (string.IsNullOrWhiteSpace(examSeriesCode))
                return 0;

            if (examSeriesCode.EndsWith("-M", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (examSeriesCode.EndsWith("-F", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 0;
        }

        public async Task<ApiResponse> StartManualSyncAsync(int numberOfDays = 2)
        {
            var userEmail = _filterParamsValues.UserEmail;

            var appUser = await _commonService._unitOfWork
                .Repository<AppUserProfile, Guid>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmailAddress == userEmail && !u.IsDeleted);

            if (appUser == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ValidationError, HttpStatusCode.BadRequest, Resource.UserNotFound);
            }

            var callerUserId = appUser.Id;

            if (_isSyncRunning)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ValidationError, HttpStatusCode.Conflict, Resource.SyncInProgress);
            }

            _isSyncRunning = true;

            // TODO - Abd-Allah: Consider validating the numberOfDays parameter to ensure it's within a reasonable range (e.g., 1 to 15 days) to prevent excessive data processing.
            NumberofDaysToSync = numberOfDays;

            BackgroundJob.Enqueue<CBTCandidatesSyncService>(
                svc => svc.ExecuteSyncAutomaticallyAsync(callerUserId));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SyncStartedInBackground
            );
        }

        public Task<ApiResponse> IsSyncRunningAsync()
        {
            return Task.FromResult(_commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, _isSyncRunning));
        }

        public async Task<ApiResponse> NotifyOldVenueCesAsync(long registrationNumber, string venueCode, string modifiedByEmail)
        {
            try
            {
                var venueUrl = await _commonService._unitOfWork
                    .Repository<Venue, long>()
                    .GetAll(x => x.Code == venueCode)
                    .AsNoTracking()
                    .Select(x => x.Url)
                    .FirstOrDefaultAsync();

                var relativePath = $"api/OesIntegration/deactivateCandidateFromPaper?registrationNumber={registrationNumber}";

                var hasVenueUrl = !string.IsNullOrWhiteSpace(venueUrl);

                if (!hasVenueUrl)
                {
                    _logger.LogWarning(
                        "No Url recorded for venue {VenueCode}; falling back to CesApiBaseUrl. RegistrationNumber={Reg}",
                        venueCode,
                        registrationNumber);
                }

                var cesEndpoint = hasVenueUrl ? $"{venueUrl.TrimEnd('/')}/{relativePath}" : relativePath; // For production, use the target venue URL

                //var cesEndpoint = relativePath; // For local test

                var apiKey = _configuration["OesSettings:OesApiKey"];
                var httpClient = _httpClientFactory.CreateClient("CesApi");

                httpClient.DefaultRequestHeaders.Remove("OesApiKey");
                httpClient.DefaultRequestHeaders.Add("OesApiKey", apiKey);
                httpClient.DefaultRequestHeaders.Remove("OesUserEmail");
                httpClient.DefaultRequestHeaders.Add("OesUserEmail", modifiedByEmail);

                var response = await httpClient.PostAsync(cesEndpoint, null);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "CES deactivation failed. RegistrationNumber={Reg}, Status={Status}",
                        registrationNumber,
                        response.StatusCode);

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.BadRequest,
                        $"The old venue ({venueCode}) returned {(int)response.StatusCode} and may still hold this candidate");
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CES deactivation exception. RegistrationNumber={Reg}",
                    registrationNumber);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    $"The old venue ({venueCode}) could not be reached and may still hold this candidate.");
            }
        }
    }
}