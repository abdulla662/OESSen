using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace OES.Services.Services.Reports
{
    public class ResultsReportsService : IResultsReportsService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private const string ItemAnalysisTimeFormat = @"h\:mm\:ss";

        public ResultsReportsService(ICommonService commonService, FilterParamsValues filterParamsValues)
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<ApiResponse> GetRawScoreResultsReportAsync(RawScoreResultsReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var cqaQuery = _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => request.FormIds.Contains(x.PaperFormId));

            HashSet<CandidateFormPair>? validPairs = null;

            if (request.Pagination.FromDate.HasValue || request.Pagination.ToDate.HasValue)
            {
                var cedQuery = _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .AsNoTracking()
                    .Where(x => request.FormIds.Contains(x.PaperFormIdActual) && x.CandidateEndedExam);

                if (request.Pagination.FromDate.HasValue)
                {
                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate >= request.Pagination.FromDate.Value);
                }

                if (request.Pagination.ToDate.HasValue)
                {
                    var endDate = request.Pagination.ToDate.Value.AddDays(1);

                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate < endDate);
                }

                validPairs =
                [.. await cedQuery
                        .Select(x =>
                            new CandidateFormPair(
                                x.CandidateId,
                                x.PaperFormIdActual))
                        .ToListAsync(cancellationToken)
                ];

                if (validPairs.Count == 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(
                            CustomCodeStatus.Success,
                            HttpStatusCode.OK,
                            Resource.RptNoCompletedSessions,
                            new RawScoreResultsPagedDto(
                                0,
                                [],
                                string.Empty,
                                []));
                }

                var candidateIds = validPairs.Select(x => x.CandidateId).ToHashSet();

                cqaQuery = cqaQuery.Where(x => candidateIds.Contains(x.CandidateId));
            }

            var rawResponses = await cqaQuery
                .Select(x =>
                    new RawScoreResponseData(
                        x.CandidateId,
                        x.PaperFormId,
                        x.CandidateNationalId,
                        x.CandidateName,
                        x.CandidateEmail,
                        x.Gender,
                        x.QuestionId,
                        x.QuestionCode,
                        x.IsCorrect,
                        x.PaperName,
                        x.PaperFormName))
                .ToListAsync(cancellationToken);

            var responses = validPairs is null ? rawResponses : [.. rawResponses.Where(x => validPairs.Contains(new CandidateFormPair(x.CandidateId, x.PaperFormId)))];

            if (responses.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.RptNoResponsesFound,
                        new RawScoreResultsPagedDto(
                            0,
                            [],
                            string.Empty,
                            []));
            }

            var totalMarksPerForm = responses
                .GroupBy(x => x.PaperFormId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(x => x.QuestionCode ?? x.QuestionId.ToString())
                        .Distinct()
                        .Count());

            var rows = responses
                .GroupBy(x =>
                    new
                    {
                        x.CandidateId,
                        x.PaperFormId
                    })
                .Select(group =>
                {
                    var first = group.First();
                    var score = group.Count(x => x.IsCorrect);
                    var totalMarks = totalMarksPerForm.TryGetValue(group.Key.PaperFormId, out var marks) ? marks : 0;
                    var percentage = totalMarks > 0 ? score * 100.0 / totalMarks : 0;

                    return new RawScoreResultRowDto(
                        first.CandidateNationalId,
                        first.CandidateName,
                        first.Gender ? Resource.Male : Resource.Female,
                        first.CandidateEmail,
                        first.PaperName,
                        first.PaperFormName,
                        totalMarks,
                        score,
                        $"{percentage:F1}%");
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(request.Pagination.SearchKey))
            {
                rows = [.. rows.Where(x => (x.Name?.Contains(request.Pagination.SearchKey, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.NationalId?.Contains(request.Pagination.SearchKey, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.Email?.Contains(request.Pagination.SearchKey, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.FormName?.Contains(request.Pagination.SearchKey, StringComparison.OrdinalIgnoreCase) ?? false))];
            }

            rows = ApplySorting(rows, request.Pagination.OrderBy);

            var totalCount = rows.Count;

            var paperName = rows.FirstOrDefault()?.PaperName ?? string.Empty;

            var formNames = rows
                .Select(x => x.FormName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Order()
                .ToList()!;

            if (!request.Pagination.PaginationOff)
            {
                rows = [.. rows
                    .Skip(request.Pagination.PageIndex * request.Pagination.PageSize)
                    .Take(request.Pagination.PageSize)];
            }

            var result = new RawScoreResultsPagedDto(
                totalCount,
                rows,
                paperName,
                formNames);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    result);
        }

        public async Task<ApiResponse> GetItemAnalysisReportAsync(ItemAnalysisReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var latestRegs = await _commonService._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(ce =>
                    request.FormIds.Contains(ce.PaperFormIdActual) &&
                    ce.CandidateEndedExam &&
                    (!request.Pagination.FromDate.HasValue || ce.ExamTrialEndDate >= request.Pagination.FromDate) &&
                    (!request.Pagination.ToDate.HasValue || ce.ExamTrialEndDate < request.Pagination.ToDate.Value.AddDays(1)))
                .Select(ce => new
                {
                    ce.CandidateId,
                    ce.RegistrationId,
                    ce.FinalScore,
                    ce.ExamTrialEndDate,
                    ce.PaperCode,
                    ce.PaperName,
                    FormName = ce.PaperFormName,
                })
                .ToListAsync(cancellationToken);

            var candidateIds = latestRegs.Select(x => x.CandidateId).ToHashSet();
            var validRegIds = latestRegs.Select(x => x.RegistrationId).ToHashSet();
            var regMeta = latestRegs.DistinctBy(x => x.RegistrationId).ToDictionary(x => x.RegistrationId);

            var answers = await _commonService._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(cq => candidateIds.Contains(cq.CandidateId) && validRegIds.Contains(cq.RegistrationId))
                .Select(cq => new
                {
                    cq.CandidateId,
                    cq.Gender,
                    cq.QuestionCode,
                    cq.QuestionId,
                    cq.IsCorrect,
                    cq.KeyAnswer,
                    cq.Response,
                    cq.ElapsedTimeInSeconds,
                    cq.RegistrationId,
                })
                .ToListAsync(cancellationToken);

            var questionIds = answers.Select(x => x.QuestionId).Distinct().ToList();

            var unscoredMap = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => questionIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Unscored
                }).ToDictionaryAsync(x => x.Id, x => x.Unscored, cancellationToken);

            var pretestQuestionIds = (await _commonService._unitOfWork
                .Repository<PaperFormBlock, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(pfb => pfb.PaperId == request.PaperId && pfb.AdaptiveSection != null && pfb.AdaptiveSection.UnScored)
                .SelectMany(pfb => pfb.Block.Questions
                .Where(bq => questionIds.Contains(bq.QuestionMetadataId))
                .Select(bq => bq.QuestionMetadataId))
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var rows = new List<ItemAnalysisRawRow>(answers.Count);

            foreach (var cq in answers)
            {
                var regInfo = regMeta[cq.RegistrationId];

                rows.Add(new ItemAnalysisRawRow(
                    cq.CandidateId,
                    cq.Gender,
                    cq.QuestionCode,
                    cq.QuestionId,
                    cq.IsCorrect,
                    cq.KeyAnswer,
                    cq.Response,
                    cq.ElapsedTimeInSeconds,
                    cq.RegistrationId,
                    regInfo.ExamTrialEndDate,
                    regInfo.FinalScore,
                    regInfo.PaperCode,
                    regInfo.PaperName,
                    regInfo.FormName,
                    unscoredMap.GetValueOrDefault(cq.QuestionId)
                ));
            }

            if (rows.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.RptNoCompletedSessions,
                    new ExportFileDto(string.Empty, string.Empty, []));
            }

            var sampledRows = rows;
            var firstRow = sampledRows[0];

            var itemResponses = sampledRows
                .ConvertAll(r => new ItemResponseData
                {
                    CandidateId = r.CandidateId,
                    QuestionCode = r.QuestionCode ?? r.QuestionId.ToString(),
                    ChosenOption = string.IsNullOrEmpty(r.Response) ? null : r.Response,
                    IsCorrect = r.IsCorrect,
                    TimeSpent = r.ElapsedTimeInSeconds.HasValue
                        ? TimeSpan.FromSeconds(r.ElapsedTimeInSeconds.Value)
                        : TimeSpan.Zero
                });

            var sessions = sampledRows
                .GroupBy(r => r.CandidateId)
                .Select(g =>
                {
                    var s = g.First();
                    var rawScore = (double)g.Count(r => r.IsCorrect);
                    var scaledScore = PsychometricsService.ParseFinalScore(s.FinalScore ?? string.Empty);
                    return new CandidateSessionData
                    {
                        CandidateId = s.CandidateId,
                        Gender = s.Gender ? 1 : 0,
                        RawScore = rawScore,
                        ScaledScore = scaledScore > 0 ? scaledScore : rawScore,
                        Duration = TimeSpan.FromSeconds(g.Sum(r => r.ElapsedTimeInSeconds ?? 0))
                    };
                })
                .ToList();

            var items = sampledRows
                .GroupBy(r => r.QuestionCode ?? r.QuestionId.ToString())
                .Select(g =>
                {
                    var first = g.First();
                    var key = g
                        .Where(r => !string.IsNullOrEmpty(r.KeyAnswer))
                        .GroupBy(r => r.KeyAnswer)
                        .OrderByDescending(x => x.Count())
                        .FirstOrDefault()?.Key ?? string.Empty;
                    return new ItemInfoData
                    {
                        QuestionCode = g.Key,
                        Status = pretestQuestionIds.Contains(first.QuestionId) || first.Unscored ? "Pretest" : "OP",
                        CorrectAnswerKey = key
                    };
                })
                .ToList();

            var org = await _commonService._unitOfWork.Repository<OrganizationStructure, long>()
                .Query(applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(o => o.Id == _filterParamsValues.OrganizationId)
                .Select(o => new { o.Name })
                .FirstOrDefaultAsync(cancellationToken);

            var raw = new RawReportData
            {
                Sessions = sessions,
                Responses = itemResponses,
                Items = items,
                ClientName = org?.Name ?? string.Empty,
                ExamCode = firstRow.PaperCode ?? string.Empty,
                ExamTitle = firstRow.PaperName ?? string.Empty,
                FormName = request.FormIds.Count > 1
                    ? Resource.RptMultipleForms
                    : firstRow.PaperFormName ?? string.Empty
            };

            var internalRequest = new ItemAnalysisReportRequest
            {
                DateFrom = request.Pagination.FromDate,
                DateTo = request.Pagination.ToDate,
            };

            var (examFormRows, itemRows, itemAllRows, responseRows, responseAllRows) = Build(raw, internalRequest);

            foreach (var r in itemRows.Concat(itemAllRows))
            {
                r.ClientName = raw.ClientName;
                r.Form = raw.FormName;
            }

            var dateRange = (request.Pagination.FromDate, request.Pagination.ToDate) switch
            {
                ({ } from, { } to) => $"{from:M/d/yyyy} - {to:M/d/yyyy}",
                ({ } from, null) => $"From {from:M/d/yyyy}",
                (null, { } to) => $"To {to:M/d/yyyy}",
                _ => Resource.All
            };

            var meta = new ItemAnalysisExportMeta(
                ExporterName: _filterParamsValues.UserEmail ?? Environment.UserName,
                PaperName: raw.ExamTitle,
                FormCode: raw.FormName,
                DateRange: dateRange);

            var bytes = WriteItemAnalysisExcel(examFormRows, itemRows, itemAllRows, responseRows, responseAllRows, meta);

            var fileNameParts = new[]
            {
                "Item_Analysis",
                BuildDateRange(request.Pagination.FromDate, request.Pagination.ToDate),
                SanitizeForFileName(raw.ExamTitle),
                SanitizeForFileName(raw.FormName)
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            var fileName = string.Join("_", fileNameParts) + ".xlsx";

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                new ExportFileDto(
                    fileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    bytes));
        }

        public async Task<ApiResponse> GetItemAnalysisPapersAsync(CancellationToken cancellationToken = default)
        {
            var papers = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => x.CandidateEndedExam && x.PaperCode != null)
                .GroupBy(x => x.PaperCode)
                .Select(x => new
                {
                    Code = x.Key!,
                    Name = x.Max(y => y.PaperName)
                })
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken);

            var result = papers
                .ConvertAll(x => new PaperLookupDto(0, x.Code, x.Name ?? x.Code));

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    result);
        }

        public async Task<ApiResponse> GetItemAnalysisFormsAsync(string paperCode, CancellationToken cancellationToken = default)
        {
            var result = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => x.CandidateEndedExam && x.PaperCode == paperCode)
                .Select(x => new
                {
                    x.PaperFormIdActual,
                    x.PaperFormName
                })
                .Distinct()
                .OrderBy(x => x.PaperFormName)
                .ToListAsync(cancellationToken);

            var forms = result
                .ConvertAll(x => new FormLookupDto(
                    x.PaperFormIdActual,
                    x.PaperFormName ?? x.PaperFormIdActual.ToString(),
                    x.PaperFormName ?? x.PaperFormIdActual.ToString()));

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    forms);
        }

        public async Task<ApiResponse> GetScoreDistributionReportAsync(ScoreDistributionReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var cqaQuery = _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => request.FormIds.Contains(x.PaperFormId));

            HashSet<CandidateFormPair>? validPairs = null;

            if (request.Pagination.FromDate.HasValue || request.Pagination.ToDate.HasValue)
            {
                var cedQuery = _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .AsNoTracking()
                    .Where(x => request.FormIds.Contains(x.PaperFormIdActual) && x.CandidateEndedExam);

                if (request.Pagination.FromDate.HasValue)
                {
                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate >= request.Pagination.FromDate.Value);
                }

                if (request.Pagination.ToDate.HasValue)
                {
                    var endDate = request.Pagination.ToDate.Value.AddDays(1);

                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate < endDate);
                }

                validPairs = [.. await cedQuery
                    .Select(x => new CandidateFormPair(x.CandidateId, x.PaperFormIdActual))
                    .ToListAsync(cancellationToken)];

                if (validPairs.Count == 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(
                            CustomCodeStatus.Success,
                            HttpStatusCode.OK,
                            Resource.RptNoCompletedSessions,
                            new ScoreDistributionReportDto(
                                string.Empty,
                                string.Empty,
                                0,
                                request.CutScore,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                [],
                                [],
                                []));
                }

                var candidateIds = validPairs.Select(x => x.CandidateId).ToHashSet();

                cqaQuery = cqaQuery.Where(x => candidateIds.Contains(x.CandidateId));
            }

            var rawResponses = await cqaQuery
                .Select(x => new ScoreDistributionRawResponse(
                    x.CandidateId,
                    x.PaperFormId,
                    x.QuestionId,
                    x.QuestionCode,
                    x.IsCorrect,
                    x.PaperName,
                    x.PaperFormName))
                .ToListAsync(cancellationToken);

            var responses = validPairs is null ? rawResponses : [.. rawResponses.Where(x => validPairs.Contains(new CandidateFormPair(x.CandidateId, x.PaperFormId)))];

            if (responses.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.NotFound,
                        new ScoreDistributionReportDto(
                            string.Empty,
                            string.Empty,
                            0,
                            request.CutScore,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            [],
                            [],
                            []));
            }

            var totalMarks = responses
                .GroupBy(x => x.PaperFormId)
                .Max(group => group.Select(x => x.QuestionCode ?? x.QuestionId.ToString()).Distinct().Count());

            var rawScores = responses
                .GroupBy(x =>
                    new
                    {
                        x.CandidateId,
                        x.PaperFormId
                    })
                .Select(group => (double)group.Count(x => x.IsCorrect))
                .ToList();

            var paperName = responses.First().PaperName ?? string.Empty;
            var formName = request.FormIds.Count == 1 ? responses.First().PaperFormName ?? string.Empty : Resource.RptMultipleForms;
            var totalCandidates = rawScores.Count;
            var mean = rawScores.Average();
            var stdDev = PsychometricsService.StdDev(rawScores);
            var median = Math.Round(PsychometricsService.Percentile(rawScores, 50), 1);
            var mode = Math.Round(PsychometricsService.Mode(rawScores), 1);
            var min = rawScores.Min();
            var max = rawScores.Max();
            var skewness = Math.Round(PsychometricsService.Skew(rawScores), 2);
            var kurtosis = Math.Round(PsychometricsService.Kurtosis(rawScores), 2);
            var passCount = rawScores.Count(x => x >= request.CutScore);
            var failCount = totalCandidates - passCount;
            var passRate = Math.Round((double)passCount / totalCandidates * 100, 1);
            var bins = BuildBins(rawScores, totalMarks, request.CutScore, totalCandidates);
            var percentiles = BuildPercentiles(rawScores);
            var bands = BuildBands(rawScores, totalMarks, request.CutScore, totalCandidates);

            var dto =
                new ScoreDistributionReportDto(
                    paperName,
                    formName,
                    totalMarks,
                    request.CutScore,
                    totalCandidates,
                    passCount,
                    failCount,
                    passRate,
                    Math.Round(mean, 1),
                    median,
                    mode,
                    Math.Round(stdDev, 1),
                    min,
                    max,
                    skewness,
                    kurtosis,
                    InterpretSkewness(skewness),
                    InterpretKurtosis(kurtosis),
                    BuildShapeInterpretation(skewness, kurtosis, passRate),
                    bins,
                    percentiles,
                    bands);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    dto);
        }

        public async Task<ApiResponse> GetResponseMatrixReportAsync(ResponseMatrixReportRequestDto request, CancellationToken cancellationToken = default)
        {
            ApplyReportResourceCulture();

            var cqaQuery = _commonService
                    ._unitOfWork
                    .Repository<CandidateQuestionsAnswers, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .AsNoTracking()
                    .Where(x => request.FormIds.Contains(x.PaperFormId));

            HashSet<long>? candidateIds = null;

            if (request.Pagination.FromDate.HasValue || request.Pagination.ToDate.HasValue)
            {
                var cedQuery =
                    _commonService._unitOfWork
                        .Repository<CandidateExamDetails, long>()
                        .Query(applySignature: false, applyOrganizationIdFilter: false)
                        .AsNoTracking()
                        .Where(x => request.FormIds.Contains(x.PaperFormIdActual) && x.CandidateEndedExam);

                if (request.Pagination.FromDate.HasValue)
                {
                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate >= request.Pagination.FromDate.Value);
                }

                if (request.Pagination.ToDate.HasValue)
                {
                    var endDate = request.Pagination.ToDate.Value.AddDays(1);

                    cedQuery = cedQuery.Where(x => x.ExamTrialEndDate < endDate);
                }

                candidateIds = [.. await cedQuery.Select(x => x.CandidateId).ToListAsync(cancellationToken)];

                if (candidateIds.Count == 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(
                            CustomCodeStatus.Success,
                            HttpStatusCode.OK,
                            Resource.RptNoCompletedSessions,
                            null);
                }

                cqaQuery = cqaQuery.Where(x => candidateIds.Contains(x.CandidateId));
            }

            var responses = await cqaQuery
                .Select(x =>
                new ResponseMatrixRow(
                    x.CandidateId,
                    x.PaperFormId,
                    x.CandidateEmail,
                    x.CandidateName,
                    x.CandidateNationalId,
                    x.QuestionId,
                    x.QuestionCode,
                    x.IsCorrect))
                .ToListAsync(cancellationToken);

            if (responses.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.NotFound,
                        null);
            }

            var formMeta = await _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .AsNoTracking()
                    .Where(x => request.FormIds.Contains(x.PaperFormIdActual))
                    .Select(x => new ResponseMatrixMeta(x.PaperName, x.PaperFormName))
                    .FirstOrDefaultAsync(cancellationToken);

            var paperName = SanitizeForFileName(formMeta?.PaperName ?? string.Empty);

            var formCode = request.FormIds.Count > 1
                ? Resource.RptMultipleForms
                : SanitizeForFileName(formMeta?.PaperFormName ?? string.Empty);

            var dateRange = BuildDateRange(request.Pagination.FromDate, request.Pagination.ToDate);

            var fileNameParts = new[] { "Response_Matrix", dateRange, paperName, formCode }.Where(x => !string.IsNullOrWhiteSpace(x));

            var fileName = string.Join("_", fileNameParts) + ".csv";

            var questionCodes = responses
                .Select(x => x.QuestionCode ?? x.QuestionId.ToString())
                .Distinct()
                .Order()
                .ToList();

            var groupedResponses = responses
                .GroupBy(x => new { x.CandidateId, x.PaperFormId })
                .OrderBy(x => x.First().CandidateName)
                .ToList();

            var sb = new StringBuilder();

            var headerCells = new[]
            {
                Resource.CandidateEmail,
                Resource.FullName,
                Resource.NationalId,
                Resource.TotalMarks
            }.Concat(questionCodes.Select(EscapeCsv));

            sb.AppendLine(string.Join(",", headerCells));

            foreach (var group in groupedResponses)
            {
                var first = group.First();
                var answerMap = group
                    .GroupBy(x => x.QuestionCode ?? x.QuestionId.ToString())
                    .ToDictionary(g => g.Key, g => g.First().IsCorrect);
                var total = group.Count(x => x.IsCorrect);
                var cells = new List<string>
                {
                    EscapeCsv(first.CandidateEmail),
                    EscapeCsv(first.CandidateName),
                    EscapeCsv(first.CandidateNationalId),
                    total.ToString()
                };

                foreach (var code in questionCodes)
                {
                    cells.Add(answerMap.TryGetValue(code, out var isCorrect) ? isCorrect ? "1" : "0" : string.Empty);
                }

                sb.AppendLine(string.Join(",", cells));
            }

            var file = new ExportFileDto(fileName, "text/csv; charset=utf-8", EncodeUtf8CsvWithBom(sb.ToString()));

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    file);
        }


        #region HelperMethods

        private static void ApplyReportResourceCulture() => Resource.Culture = CultureInfo.CurrentUICulture;

        private static byte[] EncodeUtf8CsvWithBom(string content)
        {
            var bom = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(content);
            var result = new byte[bom.Length + body.Length];
            bom.CopyTo(result, 0);
            body.CopyTo(result, bom.Length);
            return result;
        }

        private static List<RawScoreResultRowDto> ApplySorting(List<RawScoreResultRowDto> rows, string? orderBy)
        {
            return orderBy?.ToLowerInvariant() switch
            {
                "name_desc" => [.. rows.OrderByDescending(x => x.Name).ThenByDescending(x => x.FormName)],
                "nationalid_desc" => [.. rows.OrderByDescending(x => x.NationalId)],
                "score_desc" => [.. rows.OrderByDescending(x => x.Score)],
                "gender_desc" => [.. rows.OrderByDescending(x => x.Gender)],
                "formname_desc" => [.. rows.OrderByDescending(x => x.FormName)],
                "nationalid" => [.. rows.OrderBy(x => x.NationalId)],
                "score" => [.. rows.OrderBy(x => x.Score)],
                "gender" => [.. rows.OrderBy(x => x.Gender)],
                "formname" => [.. rows.OrderBy(x => x.FormName)],
                _ => [.. rows.OrderBy(x => x.Name).ThenBy(x => x.FormName)]
            };
        }

        private static List<ScoreBandDto> BuildBands(List<double> scores, int totalMarks, double cutScore, int totalCandidates)
        {
            var bandDefinitions = new[]
            {
                (0.0, 50.0),
                (50.0, 60.0),
                (60.0, 70.0),
                (70.0, 80.0),
                (80.0, (double)totalMarks)
            };

            var bands = bandDefinitions
                .Where(x => x.Item1 < totalMarks)
                .Select(x =>
                {
                    var low = x.Item1;
                    var high = Math.Min(x.Item2, totalMarks);
                    var isLast = high >= totalMarks;
                    var count = isLast ? scores.Count(s => s >= low && s <= high) : scores.Count(s => s >= low && s < high);

                    return new ScoreBandDto(
                        $"{low:F0}-{high:F0}",
                        count,
                        totalCandidates > 0 ? Math.Round((double)count / totalCandidates * 100, 1) : 0,
                        cutScore >= low && cutScore <= high,
                        0);
                })
                .ToList();

            var maxCount = bands.Max(x => x.Count);

            return
            [
                .. bands.Select(x => x with {BarFillPct = maxCount > 0 ? Math.Round((double)x.Count / maxCount* 100, 1): 0 })
            ];
        }

        private static List<ScoreDistributionBinDto> BuildBins(List<double> scores, int totalMarks, double cutScore, int totalCandidates)
        {
            const int BinWidth = 5;

            var bins = new List<ScoreDistributionBinDto>();

            var numBins = (int)Math.Ceiling((double)totalMarks / BinWidth);

            for (var i = 0; i < numBins; i++)
            {
                var low = i * BinWidth;
                var high = Math.Min(low + BinWidth, totalMarks);
                var isLast = i == numBins - 1;

                var count = isLast ? scores.Count(x => x >= low && x <= high) : scores.Count(x => x >= low && x < high);

                bins.Add(new ScoreDistributionBinDto(
                    $"{low:F0}-{high:F0}",
                    low,
                    high,
                    count,
                    totalCandidates > 0 ? Math.Round((double)count / totalCandidates * 100, 1) : 0,
                    low >= cutScore));
            }

            return bins;
        }

        private static string InterpretSkewness(double skewness)
        {
            return skewness switch
            {
                < -1.0 => Resource.RptStronglyLeftSkewed,
                < -0.5 => Resource.RptModeratelyLeftSkewed,
                <= 0.5 => Resource.RptApproximatelySymmetric,
                <= 1.0 => Resource.RptModeratelyRightSkewed,
                _ => Resource.RptStronglyRightSkewed
            };
        }

        private static string InterpretKurtosis(double kurtosis)
        {
            return kurtosis switch
            {
                < -1.0 => Resource.RptPlatykurtic,
                <= 1.0 => Resource.RptMesokurtic,
                _ => Resource.RptLeptokurtic
            };
        }

        private static List<PercentileRowDto> BuildPercentiles(List<double> scores)
        {
            var definitions = new[]
            {
                (Percentile: 10, Label: "P10", Meaning: Resource.RptPercentileP10Meaning ),
                (Percentile: 25, Label: "Q1 (P25)", Meaning: Resource.RptPercentileP25Meaning),
                (Percentile: 50, Label: "P50 (Median)", Meaning: Resource.RptPercentileP50Meaning),
                (Percentile: 75, Label: "Q3 (P75)", Meaning: Resource.RptPercentileP75Meaning),
                (Percentile: 90, Label: "P90",Meaning: Resource.RptPercentileP90Meaning)
            };

            return
            [
                .. definitions.Select(x => new PercentileRowDto(x.Label,
                Math.Round(PsychometricsService.Percentile(scores, x.Percentile), 1),x.Meaning))
            ];
        }

        private static string BuildShapeInterpretation(double skewness, double kurtosis, double passRate)
        {
            var skewPart = skewness switch
            {
                < -0.5 => Resource.RptShapeLeftSkewed,
                > 0.5 => Resource.RptShapeRightSkewed,
                _ => Resource.RptShapeSymmetric
            };

            var kurtosisPart = kurtosis switch
            {
                < -1.0 => Resource.RptShapeWideSpread,
                > 1.0 => Resource.RptShapeTightCluster,
                _ => Resource.RptShapeNormalSpread
            };

            var passPart = passRate switch
            {
                >= 80 => string.Format(Resource.RptStrongPerformance, passRate),
                >= 50 => string.Format(Resource.RptModeratePerformance, passRate),
                _ => string.Format(Resource.RptLowPerformance, passRate)
            };

            return skewPart + kurtosisPart + passPart;
        }

        private static string BuildDateRange(DateTime? fromDate, DateTime? toDate)
        {
            return (fromDate, toDate) switch
            {
                ({ } from, { } to) => $"{from:yyyyMMdd}-{to:yyyyMMdd}",
                ({ } from, null) => $"from_{from:yyyyMMdd}",
                (null, { } to) => $"to_{to:yyyyMMdd}",
                _ => string.Empty
            };
        }

        private static string SanitizeForFileName(string value)
        {
            return Regex.Replace(value, @"[^\w\-.]", "_").Trim('_');
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }

        #endregion


        #region ItemAnalysisReportBuilder

        public (
           List<ExamFormRow> examFormRows,
           List<ItemRow> itemRows,
           List<ItemRow> itemAllRows,
           List<ResponseRow> responseRows,
           List<ResponseRow> responseAllRows
        ) Build(RawReportData raw, ItemAnalysisReportRequest request)
        {
            var groups = new List<(string suffix, string label, List<CandidateSessionData> candidates)>();

            if (request.SplitByGender)
            {
                groups.Add(("-M", "Male", raw.Sessions.Where(s => s.Gender == request.GenderMaleValue).ToList()));
                groups.Add(("-F", "Female", raw.Sessions.Where(s => s.Gender == request.GenderFemaleValue).ToList()));
            }

            var allGroup = ("-A", "All", raw.Sessions);

            var examFormRows = new List<ExamFormRow>();
            var itemRows = new List<ItemRow>();
            var responseRows = new List<ResponseRow>();

            foreach (var (suffix, label, candidates) in groups)
            {
                if (candidates.Count == 0) continue;

                var candidateIds = candidates.Select(c => c.CandidateId).ToHashSet();
                var groupResponses = raw.Responses.Where(r => candidateIds.Contains(r.CandidateId)).ToList();

                examFormRows.Add(BuildExamFormRow(raw, suffix, label, candidates, groupResponses));
                itemRows.AddRange(BuildItemRows(raw, raw.ExamCode + suffix, candidates, groupResponses));
                responseRows.AddRange(BuildResponseRows(raw, raw.ExamCode + suffix, candidates, groupResponses));
            }

            // All group
            var allCandidates = allGroup.Item3;
            var allResponses = raw.Responses;
            examFormRows.Add(BuildExamFormRow(raw, allGroup.Item1, allGroup.Item2, allCandidates, allResponses));
            var itemAllRows = BuildItemRows(raw, raw.ExamCode + allGroup.Item1, allCandidates, allResponses);
            var responseAllRows = BuildResponseRows(raw, raw.ExamCode + allGroup.Item1, allCandidates, allResponses);

            return (examFormRows, itemRows, itemAllRows, responseRows, responseAllRows);
        }

        private ExamFormRow BuildExamFormRow(
            RawReportData raw,
            string suffix,
            string label,
            List<CandidateSessionData> candidates,
            List<ItemResponseData> groupResponses)
        {
            var opItems = raw.Items.Where(i => i.Status == "OP").ToList();
            var rawScores = candidates.ConvertAll(c => c.RawScore);
            var scaledScores = candidates.ConvertAll(c => c.ScaledScore);
            var durations = candidates.ConvertAll(c => c.Duration);

            var itemRows = BuildItemRowsInternal(opItems, raw.ExamCode + suffix, candidates, groupResponses);

            double alpha = 0;
            if (rawScores.Count >= 2)
            {
                var pValues = itemRows.Select(i => i.PValue).ToList();
                alpha = PsychometricsService.CronbachAlpha(pValues, rawScores);
            }

            double rawStdev = PsychometricsService.StdDev(rawScores);
            double sem = rawScores.Count > 0 ? rawStdev / Math.Sqrt(rawScores.Count) : 0;

            var opItemRows = itemRows.Where(i => i.Status == "OP").ToList();
            double meanPValue = opItemRows.Count > 0 ? opItemRows.Average(i => i.PValue) : 0;
            double meanITC = opItemRows.Any(i => i.ItemTotalCorrelation.HasValue)
                ? opItemRows.Where(i => i.ItemTotalCorrelation.HasValue).Average(i => i.ItemTotalCorrelation.Value)
                : 0;

            return new ExamFormRow
            {
                ClientName = raw.ClientName,
                ExamTitle = $"{raw.ExamTitle} - {label}",
                Exam = raw.ExamCode + suffix,
                Form = raw.FormName,
                OpItems = opItems.Count,
                N = candidates.Count,
                RawScoreMax = rawScores.Count > 0 ? rawScores.Max() : 0,
                RawScoreMean = rawScores.Count > 0 ? rawScores.Average() : 0,
                RawScoreMedian = PsychometricsService.Percentile(rawScores, 50),
                RawScoreMin = rawScores.Count > 0 ? rawScores.Min() : 0,
                RawScoreP25 = PsychometricsService.Percentile(rawScores, 25),
                RawScoreP75 = PsychometricsService.Percentile(rawScores, 75),
                RawScoreP95 = PsychometricsService.Percentile(rawScores, 95),
                RawScoreStdev = PsychometricsService.StdDev(rawScores),
                ScaledScoreMax = scaledScores.Count > 0 ? scaledScores.Max() : 0,
                ScaledScoreMean = scaledScores.Count > 0 ? scaledScores.Average() : 0,
                ScaledScoreMedian = PsychometricsService.Percentile(scaledScores, 50),
                ScaledScoreMin = scaledScores.Count > 0 ? scaledScores.Min() : 0,
                ScaledScoreP25 = PsychometricsService.Percentile(scaledScores, 25),
                ScaledScoreP75 = PsychometricsService.Percentile(scaledScores, 75),
                ScaledScoreP95 = PsychometricsService.Percentile(scaledScores, 95),
                ScaledScoreStdev = PsychometricsService.StdDev(scaledScores),
                TestTimeMax = durations.Count > 0 ? durations.Max() : TimeSpan.Zero,
                TestTimeMean = durations.Count > 0 ? TimeSpan.FromSeconds(durations.Average(d => d.TotalSeconds)) : TimeSpan.Zero,
                TestTimeMedian = PsychometricsService.PercentileTimeSpan(durations, 50),
                TestTimeMin = durations.Count > 0 ? durations.Min() : TimeSpan.Zero,
                TestTimeP25 = PsychometricsService.PercentileTimeSpan(durations, 25),
                TestTimeP75 = PsychometricsService.PercentileTimeSpan(durations, 75),
                TestTimeP95 = PsychometricsService.PercentileTimeSpan(durations, 95),
                TestTimeStdev = PsychometricsService.StdDevTimeSpan(durations),
                CronbachAlpha = alpha,
                StandardErrorMeasurement = sem,
                MeanPValue = meanPValue,
                MeanItemTotalCorrelation = meanITC
            };
        }

        private List<ItemRow> BuildItemRows(
            RawReportData raw,
            string examCode,
            List<CandidateSessionData> candidates,
            List<ItemResponseData> groupResponses)
        {
            return BuildItemRowsInternal(raw.Items, examCode, candidates, groupResponses);
        }

        private List<ItemRow> BuildItemRowsInternal(
            IEnumerable<ItemInfoData> items,
            string examCode,
            List<CandidateSessionData> candidates,
            List<ItemResponseData> groupResponses)
        {
            var candidateIds = candidates.Select(c => c.CandidateId).ToHashSet();
            var rawScoreMap = candidates.ToDictionary(c => c.CandidateId, c => c.RawScore);

            var responsesByQuestion = groupResponses
                .Where(r => candidateIds.Contains(r.CandidateId))
                .GroupBy(r => r.QuestionCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rows = new List<ItemRow>();

            foreach (var item in items)
            {
                if (!responsesByQuestion.TryGetValue(item.QuestionCode, out var itemResponses) || itemResponses.Count == 0)
                {
                    rows.Add(new ItemRow
                    {
                        ClientName = string.Empty,
                        Exam = examCode,
                        Form = string.Empty,
                        Item = item.QuestionCode,
                        Status = item.Status,
                        Key = item.CorrectAnswerKey,
                        N = 0,
                        PValue = 0,
                        PValueVariance = 0,
                        ItemTotalCorrelation = null
                    });
                    continue;
                }

                int n = itemResponses.Count;
                int correctCount = itemResponses.Count(r => r.IsCorrect);
                double pValue = (double)correctCount / n;
                double pVariance = pValue * (1 - pValue);

                // Rest scores: total raw score excluding this item
                var binary = itemResponses.ConvertAll(r => r.IsCorrect ? 1.0 : 0.0);
                var restScores = itemResponses.ConvertAll(r =>
                {
                    double total = rawScoreMap.TryGetValue(r.CandidateId, out double rs) ? rs : 0;
                    return total - (r.IsCorrect ? 1.0 : 0.0);
                });

                var itc = PsychometricsService.PointBiserial(binary, restScores);

                var times = itemResponses
                    .Where(r => r.TimeSpent > TimeSpan.Zero)
                    .Select(r => r.TimeSpent)
                    .ToList();

                rows.Add(new ItemRow
                {
                    ClientName = string.Empty,
                    Exam = examCode,
                    Form = string.Empty,
                    Item = item.QuestionCode,
                    Status = item.Status,
                    Key = item.CorrectAnswerKey,
                    N = n,
                    PValue = pValue,
                    PValueVariance = pVariance,
                    ItemTotalCorrelation = itc,
                    MeanResponseTime = times.Count > 0
                        ? TimeSpan.FromSeconds(times.Average(t => t.TotalSeconds))
                        : TimeSpan.Zero,
                    StDevResponseTime = PsychometricsService.StdDevTimeSpan(times),
                    MinResponseTime = times.Count > 0 ? times.Min() : TimeSpan.Zero,
                    MedianResponseTime = PsychometricsService.PercentileTimeSpan(times, 50),
                    MaxResponseTime = times.Count > 0 ? times.Max() : TimeSpan.Zero
                });
            }

            return rows;
        }

        private List<ResponseRow> BuildResponseRows(
            RawReportData raw,
            string examCode,
            List<CandidateSessionData> candidates,
            List<ItemResponseData> groupResponses)
        {
            var candidateIds = candidates.Select(c => c.CandidateId).ToHashSet();
            var totalScoreMap = candidates.ToDictionary(c => c.CandidateId, c => c.RawScore);
            var rows = new List<ResponseRow>();

            var responsesByQuestion = groupResponses
                .Where(r => candidateIds.Contains(r.CandidateId))
                .GroupBy(r => r.QuestionCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in raw.Items)
            {
                if (!responsesByQuestion.TryGetValue(item.QuestionCode, out var itemResponses) || itemResponses.Count == 0)
                    continue;

                int total = itemResponses.Count;
                if (total == 0) continue;

                var chosenOptions = itemResponses.ConvertAll(r => r.ChosenOption ?? string.Empty);
                var totalScores = itemResponses.ConvertAll(r => totalScoreMap.TryGetValue(r.CandidateId, out double s) ? s : 0);
                double MetricFor(string opt) => (double)chosenOptions.Count(c => c == opt) / total;

                double blankCount = chosenOptions.Count(c => string.IsNullOrEmpty(c));

                rows.Add(new ResponseRow
                {
                    ClientName = string.Empty,
                    Exam = examCode,
                    Form = raw.FormName,
                    QuestionCode = item.QuestionCode,
                    Status = item.Status,
                    Key = item.CorrectAnswerKey,
                    MetricA = MetricFor("A"),
                    MetricB = MetricFor("B"),
                    MetricC = MetricFor("C"),
                    MetricD = MetricFor("D"),
                    MetricZBlank = blankCount / total,
                    OptionTotalCorrelationA = PsychometricsService.OptionTotalCorrelation("A", chosenOptions, totalScores),
                    OptionTotalCorrelationB = PsychometricsService.OptionTotalCorrelation("B", chosenOptions, totalScores),
                    OptionTotalCorrelationC = PsychometricsService.OptionTotalCorrelation("C", chosenOptions, totalScores),
                    OptionTotalCorrelationD = PsychometricsService.OptionTotalCorrelation("D", chosenOptions, totalScores),
                    OptionTotalCorrelationZBlank = chosenOptions.Any(c => string.IsNullOrEmpty(c))
                        ? PsychometricsService.OptionTotalCorrelation(string.Empty, chosenOptions, totalScores)
                        : null
                });
            }

            return rows;
        }

        private byte[] WriteItemAnalysisExcel(
            List<ExamFormRow> examFormRows,
            List<ItemRow> itemRows,
            List<ItemRow> itemAllRows,
            List<ResponseRow> responseRows,
            List<ResponseRow> responseAllRows,
            ItemAnalysisExportMeta meta)
        {
            using var workbook = new XLWorkbook();

            WriteItemAnalysisExamFormSheet(workbook, examFormRows, meta);
            WriteItemAnalysisItemSheet(workbook, "Item", itemRows, meta);
            WriteItemAnalysisItemSheet(workbook, "ItemAll", itemAllRows, meta);
            WriteItemAnalysisResponseSheet(workbook, "Response", responseRows, meta);
            WriteItemAnalysisResponseSheet(workbook, "ResponseAll", responseAllRows, meta);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static void WriteItemAnalysisMetaHeader(IXLWorksheet ws, ItemAnalysisExportMeta meta)
        {
            ws.Cell(2, 1).Value = $"Exporter: {meta.ExporterName}";
            ws.Cell(3, 1).Value = $"Paper: {meta.PaperName}";
            ws.Cell(4, 1).Value = $"Form: {meta.FormCode}";
            ws.Cell(5, 1).Value = $"Date Range: {meta.DateRange}";
            ws.Cell(6, 1).Value = $"Export Date: {DateTime.UtcNow:M/d/yyyy}";
            ws.Cell(7, 1).Value = $"Export Time: {DateTime.UtcNow:h:mm tt}";
        }

        private static void WriteItemAnalysisHeaders(IXLWorksheet ws, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(8, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            ws.SheetView.FreezeRows(8);
        }

        private static string FormatItemAnalysisTime(TimeSpan ts) => ts.ToString(ItemAnalysisTimeFormat);

        private static void WriteItemAnalysisExamFormSheet(
            XLWorkbook workbook,
            List<ExamFormRow> rows,
            ItemAnalysisExportMeta meta)
        {
            var ws = workbook.AddWorksheet("Exam Form");
            WriteItemAnalysisMetaHeader(ws, meta);

            var headers = new[]
            {
                "ClientName", "ExamTitle", "Exam", "Form", "OpItems", "N",
                "RawScoreMax", "RawScoreMean", "RawScoreMedian", "RawScoreMin",
                "RawScoreP25", "RawScoreP75", "RawScoreP95", "RawScoreStdev",
                "ScaledScoreMax", "ScaledScoreMean", "ScaledScoreMedian", "ScaledScoreMin",
                "ScaledScoreP25", "ScaledScoreP75", "ScaledScoreP95", "ScaledScoreStdev",
                "TestTimeMax", "TestTimeMean", "TestTimeMedian", "TestTimeMin",
                "TestTimeP25", "TestTimeP75", "TestTimeP95", "TestTimeStdev",
                "CronbachAlpha", "StandardErrorMeasurement", "MeanPValue", "MeanItemTotalCorrelation"
            };

            WriteItemAnalysisHeaders(ws, headers);

            int row = 9;
            foreach (var r in rows)
            {
                int c = 1;
                ws.Cell(row, c++).Value = r.ClientName;
                ws.Cell(row, c++).Value = r.ExamTitle;
                ws.Cell(row, c++).Value = r.Exam;
                ws.Cell(row, c++).Value = r.Form;
                ws.Cell(row, c++).Value = r.OpItems;
                ws.Cell(row, c++).Value = r.N;
                ws.Cell(row, c++).Value = r.RawScoreMax;
                ws.Cell(row, c++).Value = r.RawScoreMean;
                ws.Cell(row, c++).Value = r.RawScoreMedian;
                ws.Cell(row, c++).Value = r.RawScoreMin;
                ws.Cell(row, c++).Value = r.RawScoreP25;
                ws.Cell(row, c++).Value = r.RawScoreP75;
                ws.Cell(row, c++).Value = r.RawScoreP95;
                ws.Cell(row, c++).Value = r.RawScoreStdev;
                ws.Cell(row, c++).Value = r.ScaledScoreMax;
                ws.Cell(row, c++).Value = r.ScaledScoreMean;
                ws.Cell(row, c++).Value = r.ScaledScoreMedian;
                ws.Cell(row, c++).Value = r.ScaledScoreMin;
                ws.Cell(row, c++).Value = r.ScaledScoreP25;
                ws.Cell(row, c++).Value = r.ScaledScoreP75;
                ws.Cell(row, c++).Value = r.ScaledScoreP95;
                ws.Cell(row, c++).Value = r.ScaledScoreStdev;
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeMax);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeMean);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeMedian);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeMin);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeP25);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeP75);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeP95);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.TestTimeStdev);
                ws.Cell(row, c++).Value = r.CronbachAlpha;
                ws.Cell(row, c++).Value = r.StandardErrorMeasurement;
                ws.Cell(row, c++).Value = r.MeanPValue;
                ws.Cell(row, c++).Value = r.MeanItemTotalCorrelation;
                row++;
            }
        }

        private static void WriteItemAnalysisItemSheet(
            XLWorkbook workbook,
            string sheetName,
            List<ItemRow> rows,
            ItemAnalysisExportMeta meta)
        {
            var ws = workbook.AddWorksheet(sheetName);
            WriteItemAnalysisMetaHeader(ws, meta);

            var headers = new[]
            {
                "ClientName", "Exam", "Form", "Item", "Status", "Key", "n",
                "PValue", "PValueVariance", "ItemTotalCorrelation",
                "MeanResponseTime", "StDevResponseTime", "MinResponseTime",
                "MedianResponseTime", "MaxResponseTime"
            };
            WriteItemAnalysisHeaders(ws, headers);

            int row = 9;
            foreach (var r in rows)
            {
                int c = 1;
                ws.Cell(row, c++).Value = r.ClientName;
                ws.Cell(row, c++).Value = r.Exam;
                ws.Cell(row, c++).Value = r.Form;
                ws.Cell(row, c++).Value = r.Item;
                ws.Cell(row, c++).Value = r.Status;
                ws.Cell(row, c++).Value = r.Key;
                ws.Cell(row, c++).Value = r.N;
                ws.Cell(row, c++).Value = r.PValue;
                ws.Cell(row, c++).Value = r.PValueVariance;
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.ItemTotalCorrelation);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.MeanResponseTime);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.StDevResponseTime);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.MinResponseTime);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.MedianResponseTime);
                ws.Cell(row, c++).Value = FormatItemAnalysisTime(r.MaxResponseTime);
                row++;
            }
        }

        private static void WriteItemAnalysisResponseSheet(
            XLWorkbook workbook,
            string sheetName,
            List<ResponseRow> rows,
            ItemAnalysisExportMeta meta)
        {
            var ws = workbook.AddWorksheet(sheetName);
            WriteItemAnalysisMetaHeader(ws, meta);

            var headers = new[]
            {
                "ClientName", "Exam", "Form", "QuestionCode", "Status", "Key",
                "MetricA", "MetricB", "MetricC", "MetricD", "MetricZBlank",
                "OptionTotalCorrelationA", "OptionTotalCorrelationB",
                "OptionTotalCorrelationC", "OptionTotalCorrelationD",
                "OptionTotalCorrelationZBlank"
            };

            WriteItemAnalysisHeaders(ws, headers);

            int row = 9;
            foreach (var r in rows)
            {
                int c = 1;
                ws.Cell(row, c++).Value = r.ClientName;
                ws.Cell(row, c++).Value = r.Exam;
                ws.Cell(row, c++).Value = r.Form;
                ws.Cell(row, c++).Value = r.QuestionCode;
                ws.Cell(row, c++).Value = r.Status;
                ws.Cell(row, c++).Value = r.Key;
                ws.Cell(row, c++).Value = r.MetricA;
                ws.Cell(row, c++).Value = r.MetricB;
                ws.Cell(row, c++).Value = r.MetricC;
                ws.Cell(row, c++).Value = r.MetricD;
                ws.Cell(row, c++).Value = r.MetricZBlank;
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.OptionTotalCorrelationA);
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.OptionTotalCorrelationB);
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.OptionTotalCorrelationC);
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.OptionTotalCorrelationD);
                SetItemAnalysisNullableDouble(ws.Cell(row, c++), r.OptionTotalCorrelationZBlank);
                row++;
            }
        }

        private static void SetItemAnalysisNullableDouble(IXLCell cell, double? value)
        {
            if (value.HasValue)
            {
                cell.Value = value.Value;
            }
        }

        #endregion

        #region Helper Records

        private sealed record CandidateFormPair(
            long CandidateId,
            long PaperFormId);

        private sealed record RawScoreResponseData(
            long CandidateId,
            long PaperFormId,
            string? CandidateNationalId,
            string? CandidateName,
            string? CandidateEmail,
            bool Gender,
            long QuestionId,
            string? QuestionCode,
            bool IsCorrect,
            string? PaperName,
            string? PaperFormName);

        private sealed record ScoreDistributionRawResponse(
            long CandidateId,
            long PaperFormId,
            long QuestionId,
            string? QuestionCode,
            bool IsCorrect,
            string? PaperName,
            string? PaperFormName);

        private sealed record ResponseMatrixRow(
            long CandidateId,
            long PaperFormId,
            string? CandidateEmail,
            string? CandidateName,
            string? CandidateNationalId,
            long QuestionId,
            string? QuestionCode,
            bool IsCorrect);

        private sealed record ResponseMatrixMeta(
            string? PaperName,
            string? PaperFormName);

        private sealed record ItemAnalysisRawRow(
            long CandidateId,
            bool Gender,
            string? QuestionCode,
            long QuestionId,
            bool IsCorrect,
            string? KeyAnswer,
            string? Response,
            double? ElapsedTimeInSeconds,
            long RegistrationId,
            DateTime? ExamTrialEndDate,
            string? FinalScore,
            string? PaperCode,
            string? PaperName,
            string? PaperFormName,
            bool Unscored);

        private sealed record ItemAnalysisExportMeta(
            string ExporterName,
            string PaperName,
            string FormCode,
            string DateRange);

        #endregion
    }
}
