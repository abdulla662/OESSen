
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using System.Net;

namespace OES.Services.Services.Reports
{
    public class AnalyticalReportsService : IAnalyticalReportsService
    {
        private readonly ICommonService _commonService;

        public AnalyticalReportsService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> GetFormsAsync(PaperFormsRequestDto paperFormsRequestDto, CancellationToken cancellationToken = default)
        {
            var forms = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    paperFormsRequestDto.ScheduleIds.Contains(x.ScheduleId) &&
                    paperFormsRequestDto.PaperCodes.Contains(x.PaperCode) &&
                    x.PaperFormIdActual != 0)
                .Select(x => new
                {
                    x.PaperFormIdActual,
                    x.PaperFormName
                })
                .Distinct()
                .OrderBy(x => x.PaperFormName)
                .ToListAsync(cancellationToken);

            var result = forms
                .ConvertAll(x => new PaperFormLookupDto(
                    x.PaperFormIdActual,
                    x.PaperFormName ?? x.PaperFormIdActual.ToString(),
                    x.PaperFormName ?? string.Empty));

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.FormsRetrievedSuccessfully,
                    result);
        }

        public async Task<ApiResponse> GetItemBanksAsync(CancellationToken cancellationToken = default)
        {
            var banks = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ItemBankLookupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    ParentId = x.ParentId
                })
                .ToListAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                banks
            );
        }

        public async Task<ApiResponse> GetPapersAsync(List<long> scheduleIds, CancellationToken cancellationToken = default)
        {
            var papers = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                     x.CandidateEndedExam &&
                     scheduleIds.Contains(x.ScheduleId) &&
                     x.PaperCode != null)
                 .Select(x => new PaperCodeLookupDto
                 {
                     Code = x.PaperCode!,
                     Name = x.PaperName ?? x.PaperCode!
                 })
                 .Distinct()
                 .OrderBy(x => x.Code)
                 .ToListAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                papers
            );
        }

        public async Task<ApiResponse> GetSchedulesAsync(CancellationToken cancellationToken = default)
        {
            var schedules = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateEndedExam &&
                    x.ScheduleId != 0 &&
                    x.ScheduleName != null)
                .Select(x => new ScheduleLookupDto
                {
                    Id = x.ScheduleId,
                    Name = x.ScheduleName
                        ?? x.ScheduleCode
                        ?? x.ScheduleId.ToString(),
                    Code = x.ScheduleCode ?? string.Empty
                })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                schedules
            );
        }

        public async Task<ApiResponse> GetVenuesAsync(List<long> scheduleIds, CancellationToken cancellationToken = default)
        {
            var venues = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateEndedExam &&
                    scheduleIds.Contains(x.ScheduleId) &&
                    x.VenueCode != null)
                .Select(x => new VenueLookupDto
                {
                    Code = x.VenueCode!,
                    Name = x.VenueCode!
                })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                venues);
        }

        public async Task<ApiResponse> GetCondensedTestReportAsync(CondensedTestReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var sessionQuery = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateEndedExam &&
                    request.ScheduleIds.Contains(x.ScheduleId) &&
                    request.PaperCodes.Contains(x.PaperCode));

            if (request.VenueCodes.Count > 0)
            {
                sessionQuery = sessionQuery.Where(x => request.VenueCodes.Contains(x.VenueCode));
            }

            var sessions = await sessionQuery
                .Select(s => new SessionData(
                    s.CandidateId,
                    s.PaperFormIdActual,
                    s.ScheduleName,
                    s.VenueCode,
                    s.FinalScore,
                    s.PaperTotalMarks,
                    s.CandidateExamDate))
                .ToListAsync(cancellationToken);

            if (sessions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CondensedTestReportDto
                    {
                        Schedule = request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple,
                        Venue = request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes),
                        PaperCode = string.Join(", ", request.PaperCodes)
                    });
            }

            var formIds = sessions.Select(x => x.PaperFormIdActual).ToHashSet();
            var candidateIds = sessions.Select(x => x.CandidateId).ToHashSet();

            var responses = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x =>
                    formIds.Contains(x.PaperFormId) &&
                    candidateIds.Contains(x.CandidateId) &&
                    !string.IsNullOrWhiteSpace(x.KeyAnswer))
                .Select(x => new ResponseData(
                    x.QuestionId,
                    x.QuestionCode,
                    x.CandidateId,
                    x.IsCorrect,
                    x.KeyAnswer!,
                    x.Response ?? string.Empty))
                .ToListAsync(cancellationToken);

            var attendeeIds = responses.Select(x => x.CandidateId).ToHashSet();

            var candidateScores = sessions
                .Where(x => attendeeIds.Contains(x.CandidateId) && !string.IsNullOrWhiteSpace(x.FinalScore))
                .Select(x => new CandidateScore(
                    x.CandidateId,
                    PsychometricsService.ParseFinalScore(x.FinalScore!)))
                .ToList();

            var rawScores = candidateScores.ConvertAll(x => x.RawScore);
            var candidateIdOrder = candidateScores.ConvertAll(x => x.CandidateId);
            var responsesByQuestion = responses
                .Where(x => !string.IsNullOrWhiteSpace(x.QuestionCode))
                .ToLookup(
                    x => x.QuestionCode!,
                    x => (x.CandidateId, x.IsCorrect));

            var questionIds = responses.Select(x => x.QuestionId).ToHashSet();
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x => questionIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Code,
                    cancellationToken);

            var items = BuildQuestionItems(
               responses,
               questionMetadata,
               candidateScores,
               candidateIdOrder,
               rawScores,
               responsesByQuestion,
               attendeeIds.Count);

            var report = BuildCondensedReport(
                request,
                sessions,
                attendeeIds.Count,
                rawScores,
                items);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                report);
        }

        public async Task<ApiResponse> GetItemTypeReportAsync(long? itemBankId, CancellationToken cancellationToken = default)
        {
            var itemBanks = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Select(x => new ItemBankNode(
                    x.Id,
                    x.Name,
                    x.ParentId))
                .ToListAsync(cancellationToken);

            var filterLabel = Resource.AllItemBanks;

            HashSet<long>? filteredBankIds = null;

            if (itemBankId.HasValue)
            {
                var selectedBank = itemBanks.FirstOrDefault(x => x.Id == itemBankId.Value);

                filterLabel = selectedBank?.Name ?? itemBankId.Value.ToString();

                filteredBankIds = GetDescendantBankIds(itemBankId.Value, itemBanks);
            }

            var bankNameMap = itemBanks.ToDictionary(x => x.Id, x => x.Name);

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.QuestionType != null);

            if (filteredBankIds is not null)
            {
                query = query.Where(x => filteredBankIds.Contains(x.ItemBankId));
            }

            var rawData = await query
                .Select(x => new ItemTypeRawData(
                    x.ItemBankId,
                    x.QuestionType!.Name))
                .ToListAsync(cancellationToken);

            var itemTypes = rawData
                .Select(x => x.QuestionTypeName)
                .Distinct()
                .Order()
                .ToList();

            var rows = rawData
                .GroupBy(x => bankNameMap.TryGetValue(x.ItemBankId, out var name)
                    ? name
                    : x.ItemBankId.ToString())
                .Select(group => BuildItemTypeRow(
                    group,
                    itemTypes))
                .OrderBy(x => x.ItemBankName)
                .ToList();

            var report = new ItemTypeReportDto
            {
                FilterLabel = filterLabel,
                ItemTypes = itemTypes,
                Rows = rows
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }

        public async Task<ApiResponse> GetPaperBlueprintReportAsync(PaperBlueprintReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var sessions = await GetBlueprintSessionsAsync(request, cancellationToken);

            if (sessions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        null,
                        new PaperBlueprintReportDto(
                            request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple,
                            string.Join(", ", request.PaperCodes),
                            [],
                            [])
                        );
            }

            var formIds = sessions.Select(x => x.FormId).ToHashSet();
            var paperIds = sessions.Select(x => x.PaperId).Where(x => x != 0).ToHashSet();

            var rawRows = await GetBlueprintRawRowsAsync(formIds, paperIds, cancellationToken);
            var itemTypes = rawRows.Select(x => x.QuestionTypeName).Distinct().OrderBy(x => x).ToList();
            var rows = rawRows.GroupBy(x => x.ItemBankName).Select(group => BuildBlueprintRow(group, itemTypes)).OrderBy(x => x.ItemBankName).ToList();

            var firstSession = sessions[0];

            var report = new PaperBlueprintReportDto(
                firstSession.ScheduleName ?? (request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple),
                string.Join(", ", request.PaperCodes),
                itemTypes,
                rows);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }

        public async Task<ApiResponse> GetPerformanceSummaryReportAsync(PerformanceSummaryReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => x.CandidateEndedExam &&
                            request.ScheduleIds.Contains(x.ScheduleId) &&
                            request.PaperCodes.Contains(x.PaperCode));

            if (request.VenueCodes.Count > 0)
            {
                query = query.Where(x => request.VenueCodes.Contains(x.VenueCode));
            }

            var sessions = await query.Select(x => new PerformanceSessionData(
                x.CandidateId,
                x.CandidateCode,
                x.CandidateDisplayName,
                x.PaperFormIdActual,
                x.ScheduleName,
                x.VenueCode,
                x.FinalScore,
                x.PaperTotalMarks,
                x.CandidateExamDate))
            .ToListAsync(cancellationToken);

            if (sessions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        null,
                        new PerformanceSummaryReportDto(
                            request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple,
                            request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes),
                            string.Join(", ", request.PaperCodes),
                            null,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            [],
                            [],
                            [],
                            []));
            }

            var formIds = sessions.Select(x => x.PaperFormIdActual).ToHashSet();
            var candidateIds = sessions.Select(x => x.CandidateId).ToHashSet();

            var responses = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x =>
                    formIds.Contains(x.PaperFormId) &&
                    candidateIds.Contains(x.CandidateId) &&
                    !string.IsNullOrWhiteSpace(x.KeyAnswer))
                .Select(x => new PerformanceResponseData(
                    x.QuestionId,
                    x.QuestionCode,
                    x.CandidateId,
                    x.IsCorrect,
                    x.KeyAnswer!,
                    x.Response ?? string.Empty))
                .ToListAsync(cancellationToken);

            var attendeeIds = responses.Select(x => x.CandidateId).ToHashSet();
            var filteredSessions = sessions.Where(x => attendeeIds.Contains(x.CandidateId)).ToList();

            var candidateScores = sessions.Where(x => !string.IsNullOrWhiteSpace(x.FinalScore))
                .Select(x => new CandidateScore(x.CandidateId, PsychometricsService.ParseFinalScore(x.FinalScore!)))
                .ToList();

            var rawScores = candidateScores.ConvertAll(x => x.RawScore);
            var questionIds = responses.Select(x => x.QuestionId).ToHashSet();

            var metadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => questionIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new PerformanceQuestionMetadata(
                    x.Id,
                    x.Code,
                    x.QuestionCategory != null ? x.QuestionCategory.Name : null,
                    x.ItemBank != null ? x.ItemBank.Name : null))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            var learningOutcomes = BuildLearningOutcomes(responses, metadata, attendeeIds.Count);

            var histogram = BuildHistogramBuckets(rawScores, filteredSessions.FirstOrDefault()?.PaperTotalMarks ?? 0, attendeeIds.Count);

            var atRiskStudents = BuildAtRiskStudents(
                    candidateScores,
                    filteredSessions.FirstOrDefault()?.PaperTotalMarks ?? 0,
                    attendeeIds.Count);

            var questionPerformance = BuildQuestionPerformance(responses,
                    metadata,
                    candidateScores,
                    rawScores,
                    attendeeIds.Count);

            var report = BuildPerformanceSummaryReport(request,
                    filteredSessions,
                    rawScores,
                    learningOutcomes,
                    histogram,
                    atRiskStudents,
                    questionPerformance);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }

        public async Task<ApiResponse> GetQuestionUsageReportAsync(QuestionUsageReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => x.CandidateEndedExam &&
                            request.ScheduleIds.Contains(x.ScheduleId) &&
                            request.PaperCodes.Contains(x.PaperCode));

            if (request.VenueCodes.Count > 0)
            {
                query = query.Where(x => request.VenueCodes.Contains(x.VenueCode));
            }

            var sessions = await query
                .Select(x => new QuestionUsageSessionData(
                    x.CandidateId,
                    x.PaperFormIdActual,
                    x.ScheduleName,
                    x.ScheduleCode,
                    x.VenueCode))
                .ToListAsync(cancellationToken);

            if (sessions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        null,
                        new QuestionUsageReportDto(
                            request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple,
                            request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes),
                            string.Join(", ", request.PaperCodes), [])
                        );
            }

            var formIds = sessions.Select(x => x.PaperFormIdActual).ToHashSet();
            var candidateIds = sessions.Select(x => x.CandidateId).ToHashSet();

            var responses = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x =>
                    formIds.Contains(x.PaperFormId) &&
                    candidateIds.Contains(x.CandidateId))
                .Select(x => new QuestionUsageResponseData(
                    x.QuestionId,
                    x.QuestionCode,
                    x.CandidateId,
                    x.IsCorrect,
                    !string.IsNullOrWhiteSpace(x.Response)))
                .ToListAsync(cancellationToken);

            var questionIds = responses.Select(x => x.QuestionId).ToHashSet();

            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => questionIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.ParentId,
                    x.ItemBankId
                })
                .ToListAsync(cancellationToken);

            var itemBankIds = questionMetadata.Select(x => x.ItemBankId).ToHashSet();

            var itemBankNames = itemBankIds.Count == 0 ? [] : await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => itemBankIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            var metadataMap = questionMetadata.ToDictionary(x => x.Id,
                x => new QuestionUsageMetadata(
                    x.Id,
                    x.Code,
                    x.ParentId ?? 0,
                    itemBankNames.TryGetValue(x.ItemBankId, out var bankName) ? bankName : string.Empty));

            var items = BuildQuestionUsageItems(responses, metadataMap);
            var firstSession = sessions[0];

            var distinctVenues = sessions
                .Select(x => x.VenueCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Order()
                .ToList();

            var venue = request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", distinctVenues);

            var report = new QuestionUsageReportDto(
                firstSession.ScheduleName ?? firstSession.ScheduleCode ?? (request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple),
                venue,
                string.Join(", ", request.PaperCodes),
                items);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }

        public async Task<ApiResponse> GetTestAnalysisReportAsync(TestAnalysisReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateEndedExam &&
                    request.ScheduleIds.Contains(x.ScheduleId) &&
                    request.PaperCodes.Contains(x.PaperCode));

            if (request.VenueCodes.Count > 0)
            {
                query = query.Where(x => request.VenueCodes.Contains(x.VenueCode));
            }

            var sessions = await query
                .Select(x => new TestAnalysisSessionData(
                    x.CandidateId,
                    x.PaperFormIdActual,
                    x.ScheduleName,
                    x.VenueCode,
                    x.FinalScore,
                    x.PaperTotalMarks,
                    x.PaperQuestionsCount))
                .ToListAsync(cancellationToken);

            if (sessions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        null,
                        new TestAnalysisReportDto(
                            request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple,
                            request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes),
                            string.Join(", ", request.PaperCodes),
                            [],
                            new TestAnalysisStatsDto(
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                null,
                                0,
                                null,
                                0,
                                0)));
            }

            var formIds = sessions.Select(x => x.PaperFormIdActual).ToHashSet();
            var candidateIds = sessions.Select(x => x.CandidateId).ToHashSet();

            var responses = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x =>
                    formIds.Contains(x.PaperFormId) &&
                    candidateIds.Contains(x.CandidateId) &&
                    !string.IsNullOrWhiteSpace(x.KeyAnswer))
                .Select(x => new TestAnalysisResponseData(
                    x.QuestionId,
                    x.CandidateId,
                    x.IsCorrect))
                .ToListAsync(cancellationToken);

            var attendeeIds = responses.Select(x => x.CandidateId).ToHashSet();

            var attendeeCount = attendeeIds.Count;

            var rawScores = sessions
                .Where(x =>
                    attendeeIds.Contains(x.CandidateId) &&
                    !string.IsNullOrWhiteSpace(x.FinalScore))
                .Select(x => PsychometricsService.ParseFinalScore(x.FinalScore!))
                .ToList();

            var pValues = responses
                .GroupBy(x => x.QuestionId)
                .Select(group => attendeeCount > 0 ? (double)group.Count(x => x.IsCorrect) / attendeeCount : 0)
                .ToList();

            double? cronbachAlpha = rawScores.Count >= 2 && pValues.Count >= 2 ? Math.Max(0, Math.Min(1, PsychometricsService.CronbachAlpha(pValues, rawScores))) : null;

            var firstSession = sessions[0];
            var totalMarks = firstSession.PaperTotalMarks ?? 0;
            var histogram = BuildHistogramBuckets(rawScores, totalMarks, attendeeCount);
            var mean = rawScores.Count > 0 ? rawScores.Average() : 0;

            var stdDev = PsychometricsService.StdDev(rawScores);

            var stats = new TestAnalysisStatsDto(
                attendeeCount,
                firstSession.PaperQuestionsCount > 0 ? firstSession.PaperQuestionsCount.Value : pValues.Count,
                rawScores.Count > 0 ? rawScores.Min() : 0,
                rawScores.Count > 0 ? rawScores.Max() : 0,
                Math.Round(mean, 2),
                Math.Round(PsychometricsService.Percentile(rawScores, 50), 2),
                Math.Round(PsychometricsService.Mode(rawScores), 2),
                Math.Round(stdDev, 2),
                Math.Round(stdDev * stdDev, 2),
                cronbachAlpha.HasValue ? Math.Round(cronbachAlpha.Value, 3) : null,
                attendeeCount > 0 ? Math.Round(stdDev / Math.Sqrt(attendeeCount), 3) : 0,
                cronbachAlpha.HasValue ? Math.Round(PsychometricsService.SEM(stdDev, cronbachAlpha.Value), 3) : null,
                Math.Round(PsychometricsService.Skew(rawScores), 3),
                Math.Round(PsychometricsService.Kurtosis(rawScores), 3));

            var report = new TestAnalysisReportDto(
                firstSession.ScheduleName ?? (request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple),
                firstSession.VenueCode ?? (request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes)),
                string.Join(", ", request.PaperCodes),
                histogram,
                stats);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }


        #region Helper Methods

        private static CondensedTestReportDto BuildCondensedReport(
            CondensedTestReportRequestDto request,
            IReadOnlyCollection<SessionData> sessions,
            int attendeeCount,
            IReadOnlyList<double> rawScores,
            IReadOnlyCollection<CondensedTestItemDto> items)
        {
            var first = sessions.First();

            var pValues = items.Select(x => x.CorrectPct / 100.0).ToList();

            var kr20 = rawScores.Count >= 2 ? Math.Max(0, Math.Min(1, PsychometricsService.CronbachAlpha(pValues, [.. rawScores]))) : 0;

            return new CondensedTestReportDto
            {
                Schedule = first.ScheduleName ?? (request.ScheduleIds.Count == 1
                ? request.ScheduleIds[0].ToString() : Resource.Multiple),
                Venue = first.VenueCode ?? (request.VenueCodes.Count == 0
                ? Resource.All : string.Join(", ", request.VenueCodes)),
                PaperCode = string.Join(", ", request.PaperCodes),
                ExamDate = first.CandidateExamDate,
                TotalPossiblePoints = first.PaperTotalMarks ?? 0,
                TotalStudents = attendeeCount,
                MedianScore = Math.Round(PsychometricsService.Percentile(rawScores.ToList(), 50), 2),
                MeanScore = rawScores.Count > 0 ? Math.Round(rawScores.Average(), 2) : 0,
                MaxScore = rawScores.Count > 0 ? rawScores.Max() : 0,
                MinScore = rawScores.Count > 0 ? rawScores.Min() : 0,
                StdDev = Math.Round(PsychometricsService.StdDev(rawScores.ToList()), 2),
                Kr20 = Math.Round(kr20, 3),
                RangeOfScores = rawScores.Count >= 2 ? rawScores.Max() - rawScores.Min() : 0,
                Items = [.. items]
            };
        }

        private static List<CondensedTestItemDto> BuildQuestionItems(
            IReadOnlyCollection<ResponseData> responses,
            IReadOnlyDictionary<long, string> questionMetadata,
            IReadOnlyCollection<CandidateScore> candidateScores,
            IReadOnlyCollection<long> candidateIdOrder,
            IReadOnlyList<double> rawScores,
            ILookup<string, (long CandidateId, bool IsCorrect)> responsesByQuestion,
            int attendeeCount)
        {
            var questionGroups = responses
                .GroupBy(x => x.QuestionId)
                .OrderBy(x =>
                    questionMetadata.TryGetValue(
                        x.Key,
                        out var code)
                            ? code
                            : x.Key.ToString())
                .ToList();

            var items = new List<CondensedTestItemDto>();

            for (var index = 0; index < questionGroups.Count; index++)
            {
                items.Add(
                    BuildQuestionItem(
                        questionGroups[index],
                        index + 1,
                        questionMetadata,
                        candidateScores,
                        candidateIdOrder,
                        rawScores,
                        responsesByQuestion,
                        attendeeCount));
            }

            return items;
        }

        private static CondensedTestItemDto BuildQuestionItem(
            IGrouping<long, ResponseData> group,
            int serialNumber,
            IReadOnlyDictionary<long, string> questionMetadata,
            IReadOnlyCollection<CandidateScore> candidateScores,
            IReadOnlyCollection<long> candidateIdOrder,
            IReadOnlyList<double> rawScores,
            ILookup<string, (long CandidateId, bool IsCorrect)> responsesByQuestion,
            int attendeeCount)
        {
            var rows = group.ToList();

            var first = rows[0];

            var questionCode = questionMetadata.TryGetValue(group.Key, out var metadataCode) ? metadataCode : first.QuestionCode ?? group.Key.ToString();
            var keyAnswer = first.KeyAnswer.ToUpperInvariant();
            var frequencyMap = BuildFrequencyMap(rows, attendeeCount);
            var correctPercentage = attendeeCount > 0 ? rows.Count(x => x.IsCorrect) / (double)attendeeCount * 100 : 0;
            var correctOptionFrequency = frequencyMap.TryGetValue(keyAnswer, out var value) ? value : correctPercentage;
            var nonDistractors = frequencyMap.Where(x => !string.Equals(x.Key, keyAnswer, StringComparison.OrdinalIgnoreCase) && x.Value > 0 && x.Value < correctOptionFrequency).Select(x => x.Key).ToList();

            var hasDistractorAboveCorrect = frequencyMap.Any(x => !string.Equals(
                x.Key,
                keyAnswer,
                StringComparison.OrdinalIgnoreCase) &&
                x.Value > correctOptionFrequency);

            var (upper27, lower27) = PsychometricsService.Upper27Lower27CorrectPct([.. candidateScores.Select(x => (x.CandidateId, x.RawScore))], questionCode, responsesByQuestion);
            var correctLookup = rows.Where(x => x.IsCorrect).Select(x => x.CandidateId).ToHashSet();
            var binaryAnswers = candidateIdOrder.Select(x => correctLookup.Contains(x) ? 1.0 : 0.0).ToList();
            var pointBiserial = PsychometricsService.PointBiserial(binaryAnswers, rawScores);

            return new CondensedTestItemDto
            {
                SrNo = serialNumber,
                QuestionCode = questionCode,
                CorrectAnswer = keyAnswer,
                FreqA = Math.Round(frequencyMap.GetValueOrDefault("A"), 1),
                FreqB = Math.Round(frequencyMap.GetValueOrDefault("B"), 1),
                FreqC = Math.Round(frequencyMap.GetValueOrDefault("C"), 1),
                FreqD = Math.Round(frequencyMap.GetValueOrDefault("D"), 1),
                FreqE = Math.Round(frequencyMap.GetValueOrDefault("E"), 1),
                FreqF = Math.Round(frequencyMap.GetValueOrDefault("F"), 1),
                NonDistractors = string.Join(", ", nonDistractors),
                CorrectPct = Math.Round(correctPercentage, 1),
                Upper27Pct = Math.Round(upper27, 1),
                Lower27Pct = Math.Round(lower27, 1),
                PointBiserial = pointBiserial.HasValue ? Math.Round(pointBiserial.Value, 3) : null,
                HasDistractorAboveCorrect = hasDistractorAboveCorrect
            };
        }

        private static Dictionary<string, double> BuildFrequencyMap(IReadOnlyCollection<ResponseData> responses, int attendeeCount)
        {
            var options = new[] { "A", "B", "C", "D", "E", "F" };

            return options.ToDictionary(option => option,
                option => attendeeCount > 0 ? responses.Count(x =>
                string.Equals(x.Response, option, StringComparison.OrdinalIgnoreCase)) / (double)attendeeCount * 100 : 0, StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<long> GetDescendantBankIds(long rootId, IReadOnlyCollection<ItemBankNode> itemBanks)
        {
            var childrenMap = itemBanks
                .Where(x => x.ParentId.HasValue)
                .GroupBy(x => x.ParentId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.Id).ToList());

            var descendants = new HashSet<long>();

            var queue = new Queue<long>();

            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                descendants.Add(current);

                if (!childrenMap.TryGetValue(
                        current,
                        out var children))
                {
                    continue;
                }

                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }

            return descendants;
        }

        private static ItemTypeBankRowDto BuildItemTypeRow(IGrouping<string, ItemTypeRawData> group, IReadOnlyCollection<string> itemTypes)
        {
            var counts = group
                .GroupBy(x => x.QuestionTypeName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count());

            return new ItemTypeBankRowDto
            {
                ItemBankName = group.Key,
                CountsByType = itemTypes.ToDictionary(
                    type => type,
                    type => counts.GetValueOrDefault(type))
            };
        }

        private async Task<List<BlueprintSessionData>> GetBlueprintSessionsAsync(PaperBlueprintReportRequestDto request, CancellationToken cancellationToken)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => request.ScheduleIds.Contains(x.ScheduleId) &&
                            request.PaperCodes.Contains(x.PaperCode) &&
                            x.PaperFormIdActual != 0);

            if (request.FormIds.Count > 0)
            {
                query = query.Where(x => request.FormIds.Contains(x.PaperFormIdActual));
            }

            return await query.Select(x => new BlueprintSessionData(
                x.PaperFormIdActual,
                x.PaperIdActual,
                x.ScheduleName)).Distinct().ToListAsync(cancellationToken);
        }

        private async Task<List<BlueprintRawData>> GetBlueprintRawRowsAsync(IReadOnlyCollection<long> formIds, IReadOnlyCollection<long> paperIds, CancellationToken cancellationToken)
        {
            if (formIds.Count == 0)
            {
                return [];
            }

            var responsesQuery = _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false)
                .AsNoTracking()
                .Where(x => formIds.Contains(x.PaperFormId));

            if (paperIds.Count > 0)
            {
                responsesQuery = responsesQuery.Where(x => paperIds.Contains(x.PaperId));
            }

            var questionIds = await responsesQuery.Select(x => x.QuestionId).Distinct().ToListAsync(cancellationToken);

            if (questionIds.Count == 0)
            {
                return [];
            }

            var metadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => questionIds.Contains(x.Id) &&
                            !x.IsDeleted &&
                            x.QuestionType != null)
                .Select(x => new BlueprintMetadataData(
                    x.ItemBankId,
                    x.QuestionType!.Name))
                .ToListAsync(cancellationToken);

            var itemBankIds = metadata.Select(x => x.ItemBankId).ToHashSet();

            var bankNames = itemBankIds.Count == 0 ? [] : await _commonService._unitOfWork.Repository<ItemBank, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => itemBankIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            return [.. metadata.Select(x => new BlueprintRawData(
                bankNames.TryGetValue(x.ItemBankId, out var bankName) ? bankName : Resource.Unknown,
                x.QuestionTypeName))];
        }

        private static ItemTypeBankRowDto BuildBlueprintRow(IGrouping<string, BlueprintRawData> group, IReadOnlyCollection<string> itemTypes)
        {
            var counts = group
                .GroupBy(x => x.QuestionTypeName)
                .ToDictionary(x => x.Key, x => x.Count());

            return new ItemTypeBankRowDto
            {
                ItemBankName = group.Key,
                CountsByType = itemTypes.ToDictionary(
                    itemType => itemType,
                    itemType => counts.GetValueOrDefault(itemType))
            };
        }

        private static List<LearningOutcomeDto> BuildLearningOutcomes(
            IReadOnlyCollection<PerformanceResponseData> responses,
            IReadOnlyDictionary<long, PerformanceQuestionMetadata> metadata,
            int attendeeCount
        )
        {
            return [.. responses
                .GroupBy(x => x.QuestionId)
                .Select(group =>
                {
                    metadata.TryGetValue(group.Key, out var meta);

                    return new
                    {
                        Category = meta?.CategoryName,
                        CorrectPct = attendeeCount > 0 ? (double)group.Count(x => x.IsCorrect) / attendeeCount * 100 : 0
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Category))
                .GroupBy(x => x.Category!)
                .Select(group => new LearningOutcomeDto(
                    group.Key,
                    Math.Round(group.Average(x => x.CorrectPct), 1))
                )
                .OrderBy(x => x.CategoryName)];
        }

        private static List<ScoreFrequencyBucketDto> BuildHistogramBuckets(IReadOnlyCollection<double> rawScores,
            int totalMarks,
            int attendeeCount)
        {
            if (totalMarks <= 0)
            {
                totalMarks = rawScores.Count != 0 ? (int)Math.Ceiling(rawScores.Max()) : 1;
            }

            var buckets = new List<ScoreFrequencyBucketDto>();

            for (var bucket = 0; bucket < 10; bucket++)
            {
                var percentLow = bucket * 10.0;
                var percentHigh = (bucket + 1) * 10.0;
                var rawLow = totalMarks * percentLow / 100.0;
                var rawHigh = totalMarks * percentHigh / 100.0;
                var frequency = bucket < 9 ? rawScores.Count(x => x >= rawLow && x < rawHigh) : rawScores.Count(x => x >= rawLow && x <= rawHigh);

                buckets.Add(new ScoreFrequencyBucketDto($"{percentLow:F0}–{percentHigh:F0}%",
                    $"{rawLow:F0}–{rawHigh:F0}",
                    frequency,
                    attendeeCount > 0 ? Math.Round((double)frequency / attendeeCount * 100, 1) : 0
                ));
            }

            return buckets;
        }

        public async Task<ApiResponse> GetQuestionBlockActivityReportAsync(QuestionBlockActivityReportRequestDto request, CancellationToken cancellationToken = default)
        {
            HashSet<long> _stepCategoryIds = [3, 4, 5, 10];

            var activeBlockIds = await _commonService
                ._unitOfWork
                .Repository<PaperFormBlock, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Select(x => x.BlockId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var query = _commonService
                ._unitOfWork
                .Repository<BlockQuestion, long>()
                .Query(applySignature: true, applyOrganizationIdFilter: false)
                .AsNoTracking();

            var rawData = await query
                .Join(_commonService._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .Query(applySignature: true, applyOrganizationIdFilter: false)
                    .AsNoTracking(),
                    blockQuestion => blockQuestion.QuestionMetadataId,
                    question => question.Id,
                    (blockQuestion, question) => new
                    {
                        blockQuestion.BlockId,
                        QuestionId = question.Id,
                        QuestionCode = question.Code,
                        question.QuestionCategoryId
                    })
                .Join(_commonService._unitOfWork
                    .Repository<Block, long>()
                    .Query(applySignature: true, applyOrganizationIdFilter: false)
                    .AsNoTracking(),
                    item => item.BlockId,
                    block => block.Id,
                    (item, block) => new QuestionBlockActivityRawData(
                        item.QuestionId,
                        item.QuestionCode,
                        block.Id,
                        block.Code,
                        item.QuestionCategoryId ?? 0))
                .ToListAsync(cancellationToken);

            rawData = request.ExamType switch
            {
                AdaptivePaperSubtype.MST => [.. rawData.Where(x => !_stepCategoryIds.Contains(x.QuestionCategoryId))],
                AdaptivePaperSubtype.STEP => [.. rawData.Where(x => _stepCategoryIds.Contains(x.QuestionCategoryId))],
                _ => rawData
            };

            var rows = rawData
                .ConvertAll(x => new QuestionBlockActivityRowDto(
                    x.QuestionId,
                    x.QuestionCode ?? string.Empty,
                    x.BlockId,
                    x.BlockCode ?? string.Empty,
                    activeBlockIds.Contains(x.BlockId) ? Resource.Active : Resource.Inactive))
;
            var report = new QuestionBlockActivityReportDto(request.ExamType, rows);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    report);
        }

        private static List<AtRiskStudentDto> BuildAtRiskStudents(
            IReadOnlyCollection<CandidateScore> candidateScores,
            int totalMarks,
            int attendeeCount)
        {
            if (attendeeCount < 4)
            {
                return [];
            }

            var lowest27Count = Math.Max(1, (int)Math.Ceiling(attendeeCount * 0.27));
            double ScorePercentage(double rawScore) => totalMarks > 0 ? rawScore / totalMarks * 100 : 0;

            return [.. candidateScores
                .OrderBy(x => x.RawScore)
                .Take(lowest27Count)
                .Select(x => new AtRiskStudentDto(
                    x.CandidateId.ToString(),
                    x.CandidateId.ToString(),
                    Math.Round(x.RawScore, 1),
                    Math.Round(ScorePercentage(x.RawScore), 1)))];
        }

        private static List<QuestionPerformanceDto> BuildQuestionPerformance(
            IReadOnlyCollection<PerformanceResponseData> responses,
            IReadOnlyDictionary<long, PerformanceQuestionMetadata> metadata,
            IReadOnlyCollection<CandidateScore> candidateScores,
            IReadOnlyList<double> rawScores,
            int attendeeCount)
        {
            var candidateIdOrder = candidateScores.Select(x => x.CandidateId).ToList();

            var questionGroups = responses
                .GroupBy(x => x.QuestionId)
                .OrderBy(group =>
                {
                    metadata.TryGetValue(group.Key, out var meta);
                    return meta?.Code ?? group.Key.ToString();
                })
                .ToList();

            var result = new List<QuestionPerformanceDto>();

            for (var index = 0; index < questionGroups.Count; index++)
            {
                var group = questionGroups[index];

                metadata.TryGetValue(group.Key, out var meta);

                var questionCode = meta?.Code ?? group.First().QuestionCode ?? group.Key.ToString();

                double Frequency(string option) => attendeeCount > 0 ? group.Count(x => string.Equals(x.Response, option, StringComparison.OrdinalIgnoreCase)) / (double)attendeeCount * 100 : 0;

                var correctPct = attendeeCount > 0 ? (double)group.Count(x => x.IsCorrect) / attendeeCount * 100 : 0;
                var responsesLookup = group.ToLookup(x => x.CandidateId);

                var binary = candidateIdOrder.ConvertAll(candidateId => responsesLookup[candidateId].Any(x => x.IsCorrect) ? 1.0 : 0.0);

                var pointBiserial = PsychometricsService.PointBiserial(binary, [.. rawScores]);

                result.Add(new QuestionPerformanceDto(
                    index + 1,
                    questionCode,
                    meta?.ItemBankName ?? string.Empty,
                    Math.Round(correctPct, 1),
                    0,
                    0,
                    pointBiserial.HasValue ? Math.Round(pointBiserial.Value, 3) : null,
                    0,
                    Math.Round(Frequency("A"), 1),
                    Math.Round(Frequency("B"), 1),
                    Math.Round(Frequency("C"), 1),
                    Math.Round(Frequency("D"), 1),
                    Math.Round(Frequency("E"), 1),
                    Math.Round(Frequency("F"), 1)));
            }

            return result;
        }

        private static PerformanceSummaryReportDto BuildPerformanceSummaryReport(
            PerformanceSummaryReportRequestDto request,
            IReadOnlyCollection<PerformanceSessionData> sessions,
            IReadOnlyCollection<double> rawScores,
            List<LearningOutcomeDto> learningOutcomes,
            List<ScoreFrequencyBucketDto> histogram,
            List<AtRiskStudentDto> atRiskStudents,
            List<QuestionPerformanceDto> questionPerformance)
        {
            var first = sessions.First();
            var totalQuestions = questionPerformance.Count;
            var averageScorePct = rawScores.Count > 0 ? Math.Round(rawScores.Average(), 1) : 0;
            var lowScorePct = rawScores.Count > 0 ? Math.Round(rawScores.Min(), 1) : 0;
            var highScorePct = rawScores.Count > 0 ? Math.Round(rawScores.Max(), 1) : 0;

            return new PerformanceSummaryReportDto(
                first.ScheduleName ?? (request.ScheduleIds.Count == 1 ? request.ScheduleIds[0].ToString() : Resource.Multiple),
                first.VenueCode ?? (request.VenueCodes.Count == 0 ? Resource.All : string.Join(", ", request.VenueCodes)),
                string.Join(", ", request.PaperCodes),
                first.CandidateExamDate,
                totalQuestions,
                sessions.Count,
                averageScorePct,
                lowScorePct,
                highScorePct,
                0,
                histogram,
                learningOutcomes,
                atRiskStudents,
                questionPerformance);
        }

        private static List<QuestionUsageItemDto> BuildQuestionUsageItems(
            IReadOnlyCollection<QuestionUsageResponseData> responses,
            IReadOnlyDictionary<long, QuestionUsageMetadata> metadata)
        {
            return [.. responses
                .GroupBy(x => x.QuestionId)
                .OrderBy(group =>
                {
                    metadata.TryGetValue(group.Key, out var meta);

                    return meta?.Code ?? group.Key.ToString();
                })
                .Select(group =>
                {
                    metadata.TryGetValue(group.Key, out var meta);

                    var rows = group.ToList();

                    var totalStudents = rows
                        .Select(x => x.CandidateId)
                        .Distinct()
                        .Count();

                    var correct = rows.Count(x => x.IsCorrect);
                    var incorrect = rows.Count(x => !x.IsCorrect && x.IsAttempted);
                    var notAttempted = rows.Count(x => !x.IsAttempted);
                    var correctPct = totalStudents > 0 ? (double)correct / totalStudents * 100 : 0;
                    var incorrectPct = totalStudents > 0 ? (double)incorrect / totalStudents * 100  : 0;

                    return new QuestionUsageItemDto(
                        meta?.Code ?? rows.First().QuestionCode ?? group.Key.ToString(),
                        meta?.ParentId ?? 0,
                        meta?.ItemBankName ?? string.Empty,
                        totalStudents,
                        correct,
                        incorrect,
                        notAttempted,
                        Math.Round(correctPct, 2),
                        Math.Round(incorrectPct, 2),
                        GetDifficultyLevel(correctPct / 100.0));
                })];
        }

        private static string GetDifficultyLevel(double pValue)
        {
            return pValue switch
            {
                < 0.33 => Resource.RptDifficultyHigh,
                <= 0.66 => Resource.RptDifficultyMedium,
                _ => Resource.RptDifficultyLow
            };
        }

        #endregion

        #region Helper Records

        private sealed record BlueprintMetadataData(long ItemBankId, string QuestionTypeName);

        private sealed record BlueprintRawData(string ItemBankName, string QuestionTypeName);

        private sealed record ResponseData(
            long QuestionId,
            string? QuestionCode,
            long CandidateId,
            bool IsCorrect,
            string KeyAnswer,
            string Response);

        private sealed record CandidateScore(
            long CandidateId,
            double RawScore);

        private sealed record SessionData(
            long CandidateId,
            long PaperFormIdActual,
            string? ScheduleName,
            string? VenueCode,
            string? FinalScore,
            int? PaperTotalMarks,
            DateTime? CandidateExamDate);

        private sealed record ItemBankNode(
            long Id,
            string Name,
            long? ParentId);

        private sealed record ItemTypeRawData(
            long ItemBankId,
            string QuestionTypeName);

        private sealed record BlueprintSessionData(
            long FormId,
            long PaperId,
            string? ScheduleName);

        private sealed record PerformanceSessionData(
            long CandidateId,
            string? CandidateCode,
            string? CandidateDisplayName,
            long PaperFormIdActual,
            string? ScheduleName,
            string? VenueCode,
            string? FinalScore,
            int? PaperTotalMarks,
            DateTime? CandidateExamDate);

        private sealed record PerformanceResponseData(long QuestionId,
            string? QuestionCode,
            long CandidateId,
            bool IsCorrect,
            string KeyAnswer,
            string Response);

        private sealed record PerformanceQuestionMetadata(
            long Id,
            string? Code,
            string? CategoryName,
            string? ItemBankName);

        private sealed record QuestionUsageSessionData(
            long CandidateId,
            long PaperFormIdActual,
            string? ScheduleName,
            string? ScheduleCode,
            string? VenueCode);

        private sealed record QuestionUsageResponseData(
            long QuestionId,
            string? QuestionCode,
            long CandidateId,
            bool IsCorrect,
            bool IsAttempted);

        private sealed record QuestionUsageMetadata(
            long Id,
            string? Code,
            long ParentId,
            string? ItemBankName);

        private sealed record TestAnalysisSessionData(
            long CandidateId,
            long PaperFormIdActual,
            string? ScheduleName,
            string? VenueCode,
            string? FinalScore,
            int? PaperTotalMarks,
            int? PaperQuestionsCount);

        private sealed record TestAnalysisResponseData(
            long QuestionId,
            long CandidateId,
            bool IsCorrect);

        private sealed record QuestionBlockActivityRawData(
            long QuestionId,
            string? QuestionCode,
            long BlockId,
            string? BlockCode,
            long QuestionCategoryId);

        #endregion
    }
}
