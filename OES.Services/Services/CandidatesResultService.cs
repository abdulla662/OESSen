using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OES.Core.Entities;
using OES.Core.Entities.CTRExam;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Core.Entities.Views.Results;
using OES.Helper;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.CTRExam;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.QuestionIndicator;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;
using OES.Services.Helpers;
using SharedHelper.Crypto;
using SharedHelper.Dtos;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Data;
using System.Net;
using System.Text.Json;
using Resource = OES.Helper.ResourceFiles.Resource;

namespace OES.Services.Services
{
    public class CandidatesResultService : ICandidatesResultService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IConfiguration _configuration;
        private readonly IEquationTemplateService _equationTemplateService;

        public CandidatesResultService(ICommonService commonService, FilterParamsValues filterParamsValues, IConfiguration configuration, IEquationTemplateService equationTemplateService)
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _configuration = configuration;
            _equationTemplateService = equationTemplateService;
        }

        public async Task<ApiResponse> GetAllCandidateQuestionsAnswersAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query()
                .AsNoTracking();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey))
            {
                string searchKey = paginationSearchModel.SearchKey.ToLower();

                if (paginationSearchModel.SearchInName)
                {
                    query = query.Where(a => a.CandidateCode.ToLower().Contains(searchKey));
                }
                else
                {
                    query = query.Where(result =>
                        result.CandidateName.ToLower().Contains(searchKey) ||
                        result.VenueCode.ToLower().Contains(searchKey) ||
                        result.CandidateNationalId.Contains(searchKey) ||
                        result.CandidatePhoneNumber.ToLower().Contains(searchKey) ||
                        result.CandidateEmail.ToLower().Contains(searchKey)
                    );
                }
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= paginationSearchModel.ToDate.Value);
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ?
                query.OrderByDescending(x => x.CreatedAt) :
                query.OrderBy(x => x.CreatedAt);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultFound
                );
            }

            if (!paginationSearchModel.PaginationOff)
            {
                int pageSize = paginationSearchModel.PageSize > 0 ? paginationSearchModel.PageSize : 10;

                query = query
                    .Skip(paginationSearchModel.PageIndex * pageSize)
                    .Take(pageSize);
            }

            var candidateQuestionsAnswers = await query.ToListAsync();

            var candidateQuestionsAnswersDtos = candidateQuestionsAnswers
                .ConvertAll(MapToDto);

            var tableData = new CustomTableData<GetCandidateQuestionsAnswersDto>(candidateQuestionsAnswersDtos, totalRecords);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesFetchedSuccessfully,
                tableData
            );
        }

        public async Task<ApiResponse> AddCandidatesQuestionsAnswersAsync(List<AddCandidateQuestionsAnswersDto> candidateQuestionsAnswersDtos)
        {
            if (candidateQuestionsAnswersDtos == null || candidateQuestionsAnswersDtos.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.CandidateQuestionsAnswersListCannotBeEmpty
                );
            }

            var candidateIds = candidateQuestionsAnswersDtos.Select(dto => dto.OriginalUserId).Distinct().ToList();

            var registrationIds = candidateQuestionsAnswersDtos.Select(dto => dto.RegistrationId).Distinct().ToList();

            var candidatesFromDb = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .Query()
                .AsNoTracking()
                .Where(c => candidateIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.CandidateCode,
                    c.NationalId,
                    c.Name,
                    c.Email,
                    c.Mobile,
                    c.Gender
                })
                .ToListAsync();

            var candidateMap = candidatesFromDb.ToDictionary(c => c.Id);

            var schedulePaperCandidatesData = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query()
                .AsNoTracking()
                .Where(spc =>
                    candidateIds.Contains(spc.CandidateId) &&
                    registrationIds.Contains(spc.RegistrationNumber) &&
                    !spc.IsDeleted)
                .Select(spc => new
                {
                    spc.CandidateId,
                    spc.RegistrationNumber,
                    spc.PaperFormId,
                    spc.SchedulePaperId,
                    spc.ModeficationUser,
                    spc.CreationDate,
                    spc.ModeficationDate,
                    spc.IsDeleted,
                    spc.DeletedDate,
                    spc.IsActive,
                    ScheduleId = spc.SchedulePaper.ScheduleMetadata.Id,
                    PaperFormName = spc.PaperForm.Name,
                    PaperId = spc.PaperForm.Paper.Id,
                    PaperName = spc.PaperForm.Paper.Name,
                })
                .ToListAsync();

            var schedulePaperCandidateMap = schedulePaperCandidatesData
                .GroupBy(spc => (spc.CandidateId, spc.RegistrationNumber))
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // Query ONLY potential duplicates — not the full table
            var existingAnswers = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query()
                .AsNoTracking()
                .Where(x => candidateIds.Contains(x.CandidateId) && registrationIds.Contains(x.RegistrationId))
                .Select(x => new
                {
                    x.CandidateId,
                    x.RegistrationId,
                    x.QuestionId,
                    x.CandidateExamTrialId
                })
                .ToListAsync();

            // Use HashSet for O(1) duplicate lookups instead of O(N) List.Any()
            var existingSet = existingAnswers
                .Select(x => (x.CandidateId, x.RegistrationId, x.QuestionId, x.CandidateExamTrialId))
                .ToHashSet();

            // Filter out duplicates - keep only new records
            var newAnswers = candidateQuestionsAnswersDtos
                .Where(dto => !existingSet.Contains((dto.OriginalUserId, dto.RegistrationId, dto.OriginalQuestionId, dto.CandidateExamTrialId)))
                .ToList();

            // If all records already exist, return success with count 0
            if (newAnswers.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.AllAnswersAlreadyExistInDatabase,
                    0
                );
            }

            // Convert DTOs to entities
            var candidateQuestionsAnswers = newAnswers.ConvertAll(dto =>
            {
                candidateMap.TryGetValue(dto.OriginalUserId, out var candidate);

                schedulePaperCandidateMap.TryGetValue(
                    (dto.OriginalUserId, dto.RegistrationId),
                    out var spc
                );

                return new CandidateQuestionsAnswers
                {
                    CandidateId = dto.OriginalUserId,
                    CandidateCode = candidate?.CandidateCode ?? dto.CandidateCode,
                    CandidateNationalId = candidate?.NationalId ?? dto.CandidateNationalId,
                    CandidateName = candidate?.Name ?? dto.CandidateName,
                    CandidateEmail = candidate?.Email ?? dto.CandidateEmail,
                    CandidatePhoneNumber = candidate?.Mobile ?? dto.CandidatePhoneNumber,
                    Gender = candidate != null ? candidate.Gender != 0 : dto.Gender,
                    ClientCandidateId = candidate?.NationalId ?? dto.CandidateNationalId,

                    PaperFormId = spc?.PaperFormId ?? dto.OriginalPaperFormId,
                    PaperFormName = spc?.PaperFormName ?? dto.PaperFormName,
                    PaperId = spc?.PaperId ?? dto.OriginalPaperId,
                    PaperName = spc?.PaperName ?? dto.PaperName,
                    ScheduleId = spc?.ScheduleId ?? dto.OriginalScheduleId,
                    SchedulePaperId = spc?.SchedulePaperId ?? dto.OriginalSchedulePaperId,

                    ModeficationUser = spc?.ModeficationUser,
                    CreationDate = spc?.CreationDate ?? DateTimeHelper.Now,
                    ModeficationDate = spc?.ModeficationDate ?? DateTimeHelper.Now,
                    IsDeleted = spc?.IsDeleted ?? false,
                    DeletedDate = spc?.DeletedDate,
                    IsActive = spc?.IsActive ?? true,

                    QuestionId = dto.OriginalQuestionId,
                    ParentQuestionId = dto.ParentQuestionId,
                    IsRootQuestion = dto.IsRootQuestion,
                    QuestionCode = dto.QuestionCode,
                    QuestionType = dto.QuestionType,
                    QuestionSubject = dto.QuestionSubject,
                    QuestionScore = dto.QuestionScore,
                    IsAutoCorrectable = dto.IsAutoCorrectable,
                    EvaluationStatus = dto.IsAutoCorrectable
                        ? EvaluationStatus.NotRequired
                        : EvaluationStatus.Required,
                    AnswerId = dto.AnswerId,
                    AnswerText = dto.AnswerText,
                    AnswerIds = dto.AnswerIds,
                    KeyAnswer = dto.KeyAnswer,
                    Response = dto.Response,
                    Visited = dto.Visited,
                    Answered = dto.Answered,
                    MarkedForReview = dto.MarkedForReview,
                    ElapsedTimeInSeconds = dto.ElapsedTimeInSeconds,
                    AnswerCreatedAt = dto.AnswerCreatedAt,
                    AnswerLastModifiedAt = dto.AnswerLastModifiedAt,
                    CreatedAt = dto.CreatedAt ?? DateTimeHelper.Now,
                    ExamStartDate = dto.ExamStartDate,
                    ExamEndDate = dto.ExamEndDate,
                    VenueId = dto.VenueId,
                    VenueCode = dto.VenueCode,
                    MarksObtained = dto.MarksObtained,
                    ModelAnswerIds = dto.ModelAnswerIds,
                    SectionId = dto.SectionId,
                    SectionName = dto.SectionName,
                    SectionType = dto.SectionType,
                    TrialNumber = dto.TrialNumber,
                    CandidateExamTrialId = dto.CandidateExamTrialId,
                    RegistrationId = dto.RegistrationId,
                    IsCorrect = dto.IsCorrect,
                    TCID = dto.TCID,
                };
            });

            // Save only new records
            _commonService._unitOfWork.Repository<CandidateQuestionsAnswers, long>().AddRangAsync(candidateQuestionsAnswers);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                int duplicateCount = candidateQuestionsAnswersDtos.Count - newAnswers.Count;

                string message = duplicateCount > 0
                    ? $"{newAnswers.Count} new answers added. {duplicateCount} duplicates skipped."
                    : string.Format(Resource.CountOfCandidateQuestionsAnswersAddedSuccessfully, newAnswers.Count);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    message,
                    newAnswers.Count
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToAddCandidateQuestionsAnswers
            );
        }

        public async Task<ApiResponse> AddCandidateExamDetailsAsync(List<AddCandidateExamDetailsDto> candidateExamDetailsDtos)
        {
            if (candidateExamDetailsDtos == null || candidateExamDetailsDtos.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.CandidateExamDetailsListCannotBeEmpty
                );
            }

            var incomingCandidateIds = candidateExamDetailsDtos
                .Select(dto => dto.OriginalUserId)
                .Distinct()
                .ToList();

            var candidatesFromDb = await _commonService
                 ._unitOfWork
                 .Repository<Candidate, long>()
                 .Query()
                 .AsNoTracking()
                 .Where(c => incomingCandidateIds.Contains(c.Id))
                 .Select(c => new
                 {
                     c.Id,
                     c.CandidateCode,
                     c.NationalId,
                     c.Name,
                     c.Email,
                     c.Mobile,
                     c.CreationDate
                 })
                 .ToListAsync();

            var candidateMap = candidatesFromDb.ToDictionary(c => c.Id);

            var registrationNumbers = candidateExamDetailsDtos
                .Select(dto => dto.RegistrationId)
                .Distinct()
                .ToList();

            var schedulePaperCandidatesData = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query()
                .AsNoTracking()
                .Where(spc =>
                    incomingCandidateIds.Contains(spc.CandidateId) &&
                    registrationNumbers.Contains(spc.RegistrationNumber) &&
                    !spc.IsDeleted)
                .Select(spc => new
                {
                    spc.Id,
                    spc.CandidateId,
                    spc.RegistrationNumber,
                    spc.PaperFormId,
                    spc.CandidateExamDate,
                    spc.CreationUser,
                    spc.ModeficationUser,
                    spc.CreationDate,
                    spc.ModeficationDate,
                    spc.IsDeleted,
                    spc.DeletedDate,
                    spc.IsActive,

                    ScheduleId = spc.SchedulePaper.ScheduleMetadata.Id,
                    ScheduleName = spc.SchedulePaper.ScheduleMetadata.Name,
                    ScheduleCode = spc.SchedulePaper.ScheduleMetadata.Code,

                    PaperFormIdActual = spc.PaperForm.Id,
                    PaperFormName = spc.PaperForm.Name,
                    PaperFormIsDeleted = spc.PaperForm.IsDeleted,

                    PaperIdActual = spc.PaperForm.Paper.Id,
                    OriginalPaperId = spc.PaperForm.Paper.Id,
                    PaperName = spc.PaperForm.Paper.Name,
                    PaperCode = spc.PaperForm.Paper.Code,
                    PaperDuration = spc.PaperForm.Paper.Duration,
                    PaperQuestionsCount = spc.PaperForm.Paper.QuestionsCount,
                    PaperTotalMarks = spc.PaperForm.Paper.TotalMarks,
                    PaperStartDate = spc.SchedulePaper.StartDate,
                    PaperEndDate = spc.SchedulePaper.EndDate,
                    PaperLanguage = spc.PaperForm.Paper.Language.Name,
                    PaperAllowInstantResult = spc.PaperForm.Paper.AllowInstantResult,
                    PaperType = spc.PaperForm.Paper.Type,
                    PaperIsDeleted = spc.PaperForm.Paper.IsDeleted,
                })
                .ToListAsync();

            var schedulePaperCandidateMap = schedulePaperCandidatesData
                .GroupBy(spc => (spc.CandidateId, spc.RegistrationNumber))
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            var registrationIds = candidateExamDetailsDtos
                .Select(dto => dto.RegistrationId)
                .Distinct()
                .ToList();

            var candidateNationalIds = candidateExamDetailsDtos
                .Select(dto => dto.CandidateNationalId)
                .Distinct()
                .ToList();

            // Query ONLY potential duplicates — not the full table
            var existingDetails = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query()
                .AsNoTracking()
                .Where(x => registrationIds.Contains(x.RegistrationId) && candidateNationalIds.Contains(x.CandidateNationalId))
                .Select(x => new
                {
                    x.RegistrationId,
                    x.CandidateNationalId,
                    x.CandidateCenterRegistrationCode,
                    x.ExamTrialId
                })
                .ToListAsync();

            // Use HashSet for O(1) duplicate lookups
            var existingSet = existingDetails
                .Select(x => (x.RegistrationId, x.CandidateNationalId, x.CandidateCenterRegistrationCode, x.ExamTrialId))
                .ToHashSet();

            // Filter out duplicates - keep only new records
            var newDetails = candidateExamDetailsDtos
                .Where(dto => !existingSet.Contains((dto.RegistrationId, dto.CandidateNationalId, dto.CandidateCenterRegistrationCode, dto.ExamTrialId)))
                .ToList();

            // If all records already exist, return success with count 0
            if (newDetails.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.AllCandidateExamDetailsAlreadyExistInDatabase,
                    0
                );
            }

            var candidateExamDetails = newDetails.ConvertAll(dto =>
            {
                candidateMap.TryGetValue(dto.OriginalUserId, out var candidate);

                schedulePaperCandidateMap.TryGetValue(
                    (dto.OriginalUserId, dto.RegistrationId),
                    out var spc
                );

                return new CandidateExamDetails
                {
                    CandidateCode = candidate?.CandidateCode ?? dto.CandidateCode,
                    CandidateNationalId = candidate?.NationalId ?? dto.CandidateNationalId,
                    CandidateDisplayName = !string.IsNullOrWhiteSpace(candidate?.Name)
                        ? candidate.Name
                        : (string.IsNullOrWhiteSpace(dto.CandidateDisplayName) ? null : dto.CandidateDisplayName),
                    CandidateEmail = candidate?.Email ?? dto.CandidateEmail,
                    CandidatePhoneNumber = candidate?.Mobile ?? dto.CandidatePhoneNumber,
                    CandidateCreatedAt = candidate?.CreationDate ?? dto.CandidateCreatedAt,

                    UserId = dto.OriginalUserId,
                    CandidateId = dto.OriginalUserId,

                    CandidatePaperId = dto.CandidatePaperId,
                    PaperFormId = dto.PaperFormId,
                    CandidateExamDate = spc?.CandidateExamDate ?? dto.CandidateExamDate,

                    ScheduleId = spc?.ScheduleId ?? dto.OriginalScheduleId,
                    ScheduleName = spc?.ScheduleName ?? dto.ScheduleName,
                    ScheduleCode = spc?.ScheduleCode ?? dto.ScheduleCode,

                    PaperFormIdActual = dto.PaperFormIdActual,
                    PaperFormName = spc?.PaperFormName ?? dto.PaperFormName,
                    PaperFormIsDeleted = spc?.PaperFormIsDeleted ?? dto.PaperFormIsDeleted,

                    PaperIdActual = dto.PaperIdActual,
                    OriginalPaperId = spc != null ? (long)spc.OriginalPaperId : dto.OriginalPaperId,
                    PaperName = spc?.PaperName ?? dto.PaperName,
                    PaperCode = spc?.PaperCode ?? dto.PaperCode,
                    PaperDuration = spc?.PaperDuration ?? dto.PaperDuration,
                    PaperQuestionsCount = spc?.PaperQuestionsCount ?? dto.PaperQuestionsCount,
                    PaperTotalMarks = spc != null ? (int?)spc.PaperTotalMarks : dto.PaperTotalMarks,
                    PaperStartDate = spc != null ? spc.PaperStartDate.ToDateTime(TimeOnly.MinValue) : dto.PaperStartDate ?? DateTimeHelper.Now,
                    PaperEndDate = spc != null ? spc.PaperEndDate.ToDateTime(TimeOnly.MinValue) : dto.PaperEndDate ?? DateTimeHelper.Now,
                    PaperLanguage = spc?.PaperLanguage ?? dto.PaperLanguage,
                    PaperAllowInstantResult = spc?.PaperAllowInstantResult ?? dto.PaperAllowInstantResult,
                    PaperType = spc?.PaperType ?? dto.PaperType ?? null,
                    PaperIsDeleted = spc?.PaperIsDeleted ?? dto.PaperIsDeleted,

                    CreationUser = spc?.CreationUser,
                    ModeficationUser = spc?.ModeficationUser,
                    CreationDate = spc?.CreationDate ?? DateTimeHelper.Now,
                    ModeficationDate = spc?.ModeficationDate ?? DateTimeHelper.Now,
                    IsDeleted = spc?.IsDeleted ?? false,
                    DeletedDate = spc?.DeletedDate,
                    IsActive = spc?.IsActive ?? true,

                    CandidateCenterRegistrationCode = dto.CandidateCenterRegistrationCode,
                    RegistrationId = dto.RegistrationId,
                    FinalScore = dto.FinalScore ?? null,
                    ExamTrialId = dto.ExamTrialId,
                    TrialNumber = dto.TrialNumber,
                    CandidateStartedExam = dto.CandidateStartedExam,
                    CandidateEndedExam = dto.CandidateEndedExam,
                    ExamTrialStartDate = dto.ExamTrialStartDate ?? DateTimeHelper.Now,
                    ExamTrialEndDate = dto.ExamTrialEndDate ?? DateTimeHelper.Now,
                    CandidateCurrentlyInExam = dto.CandidateCurrentlyInExam,

                    // Flags
                    HasNoTrial = dto.HasNoTrial,

                    TrackingLogsJSON = dto.TrackingLogsJSON,
                    ExamEndedBySystem = dto.ExamEndedBySystem,

                    // Computed Fields
                    TotalQuestionsInDatabase = dto.TotalQuestionsInDatabase,
                    QuestionsAnsweredCount = dto.QuestionsAnsweredCount,
                    QuestionsMarkedForReview = dto.QuestionsMarkedForReview,
                    QuestionsVisited = dto.QuestionsVisited,
                    TotalElapsedTimeInSeconds = dto.TotalElapsedTimeInSeconds,
                    ReviewStatus = dto.ExamEndedBySystem ? ReviewStatus.Pending : ReviewStatus.NotApplicable,
                    ReviewedAt = null,
                    ReviewedBy = null,
                    SentToCTR = false,
                    SentToCTRAt = null,
                    SentToCTRBy = null,
                    JobId = 0,
                    Synced = false,

                    // Organization / venue (inherited in BaseEntity)
                    OrganizationId = dto.OrganizationId,
                    OrganizationSignature = string.IsNullOrWhiteSpace(dto.OrganizationSignature) ? MiscConstants.OrganizationSignature : dto.OrganizationSignature,
                    VenueId = dto.VenueId,
                    VenueCode = dto.VenueCode,
                    IsDemo = dto.IsDemo
                };
            });

            _commonService._unitOfWork.Repository<CandidateExamDetails, long>().AddRangAsync(candidateExamDetails);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                int duplicateCount = candidateExamDetailsDtos.Count - newDetails.Count;

                string message = duplicateCount > 0
                    ? $"{newDetails.Count} new exam details added. {duplicateCount} duplicates skipped."
                    : string.Format(Resource.CountOfCandidateExamDetailsAddedSuccessfully, newDetails.Count);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    message,
                    newDetails.Count
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToAddCandidateExamDetails
            );
        }
        public async Task<ApiResponse> GetUnfinishedCandidatesForReviewAsync(PaginationSearchModel paginationSearchModel)
        {
            List<int> reviewStatusList = [];

            if (paginationSearchModel.FilterObj is JsonElement filterElement && filterElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in filterElement.EnumerateArray())
                {
                    if (item.TryGetInt32(out int statusValue) && Enum.IsDefined(typeof(ReviewStatus), statusValue))
                    {
                        reviewStatusList.Add(statusValue);
                    }
                }
            }

            string? reviewStatus = reviewStatusList.Count > 0
                ? string.Join(",", reviewStatusList)
                : null;

            var parameters = new List<(string Name, object Value)>
            {
                // TODO - Salah: Avoid literal strings for stored procedure parameters sample
                ($"p_{nameof(paginationSearchModel.PageIndex)}", paginationSearchModel.PageIndex),
                ("p_PageSize", paginationSearchModel.PageSize),
                ("p_PaginationOff", paginationSearchModel.PaginationOff ? 1 : 0),
                ("p_SearchKey", paginationSearchModel.SearchKey ?? (object)DBNull.Value),
                ("p_SearchInName", paginationSearchModel.SearchInName ? 1 : 0),
                ("p_FromDate", paginationSearchModel.FromDate ?? (object)DBNull.Value),
                ("p_ToDate", paginationSearchModel.ToDate ?? (object)DBNull.Value),
                ("p_OrderBy", paginationSearchModel.OrderBy ?? (object)DBNull.Value),
                ("p_ReviewStatusList", reviewStatus ?? (object)DBNull.Value)
            };

            var result = await _commonService._unitOfWork.ExecuteStoredProcedureAsync<GetUnfinishedCandidatesResultDto>(
                $"sp_GetTrackingAndReviewingDetails",
                parameters
            );

            var totalRecords = result.FirstOrDefault()?.TotalRecords ?? 0;

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoUnfinishedCandidatesFound
                );
            }

            var candidateExamDetailsDtos = result.Cast<GetCandidateExamDetailsDto>().ToList();

            var tableData = new CustomTableData<GetCandidateExamDetailsDto>(candidateExamDetailsDtos, totalRecords);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.UnfinishedCandidatesFetchedSuccessfully,
                tableData
            );
        }

        public async Task<ApiResponse> GetCandidateResultByIdAsync(long id)
        {
            var candidateResult = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .GetObjAsync(x => x.CandidateId == id);

            if (candidateResult == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound
                );
            }

            var candidateResultDto = MapToDto(candidateResult);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidateFetchedSuccessfully,
                candidateResultDto
            );
        }

        private static GetCandidateQuestionsAnswersDto MapToDto(CandidateQuestionsAnswers result)
        {
            return new GetCandidateQuestionsAnswersDto
            {
                CandidateId = result.CandidateId,
                CandidateCode = result.CandidateCode,
                CandidateNationalId = result.CandidateNationalId,
                CandidateName = result.CandidateName,
                Gender = result.Gender,
                CandidateEmail = result.CandidateEmail,
                CandidatePhoneNumber = result.CandidatePhoneNumber,
                QuestionId = result.QuestionId,
                ParentQuestionId = result.ParentQuestionId,
                IsRootQuestion = result.IsRootQuestion,
                QuestionCode = result.QuestionCode,
                QuestionType = result.QuestionType,
                QuestionSubject = result.QuestionSubject,
                QuestionScore = result.QuestionScore,
                IsAutoCorrectable = result.IsAutoCorrectable,
                AnswerId = result.AnswerId,
                AnswerText = result.AnswerText,
                AnswerIds = result.AnswerIds,
                KeyAnswer = result.KeyAnswer,
                Response = result.Response,
                Visited = result.Visited,
                Answered = result.Answered,
                MarkedForReview = result.MarkedForReview,
                ElapsedTimeInSeconds = result.ElapsedTimeInSeconds,
                PaperFormId = result.PaperFormId,
                PaperFormName = result.PaperFormName,
                AnswerCreatedAt = result.AnswerCreatedAt,
                AnswerLastModifiedAt = result.AnswerLastModifiedAt,
                CreatedAt = result.CreatedAt,
                VenueId = result.VenueId,
                VenueCode = result.VenueCode,
                MarksObtained = result.MarksObtained,
                ModelAnswerIds = result.ModelAnswerIds,
                PaperId = result.PaperId,
                PaperName = result.PaperName,
                SectionId = result.SectionId,
                TrialNumber = result.TrialNumber,
                RegistrationId = result.RegistrationId,
                CandidateExamTrialId = result.CandidateExamTrialId,
                ClientCandidateId = result.ClientCandidateId,
                IsCorrect = result.IsCorrect,
                ExamStartDate = result.ExamStartDate,
                ExamEndDate = result.ExamEndDate,
                TCID = result.TCID
            };
        }

        private static GetCandidateExamDetailsDto MapToCandidateExamDetailsDto(CandidateExamDetails candidate)
        {
            return new GetCandidateExamDetailsDto
            {
                Id = candidate.Id,
                CandidateId = candidate.CandidateId,
                RegistrationId = candidate.RegistrationId,
                CandidateCode = candidate.CandidateCode,
                CandidateNationalId = candidate.CandidateNationalId,
                CandidateDisplayName = candidate.CandidateDisplayName,
                CandidateEmail = candidate.CandidateEmail,
                CandidatePhoneNumber = candidate.CandidatePhoneNumber,
                PaperIdActual = candidate.PaperIdActual,
                OriginalPaperId = candidate.OriginalPaperId,
                PaperName = candidate.PaperName,
                PaperCode = candidate.PaperCode,
                PaperStartDate = candidate.PaperStartDate,
                PaperEndDate = candidate.PaperEndDate,
                CandidateExamDate = candidate.CandidateExamDate,
                PaperFormIdActual = candidate.PaperFormIdActual,
                PaperFormName = candidate.PaperFormName,
                ScheduleIdActual = candidate.ScheduleId,
                ScheduleName = candidate.ScheduleName,
                ScheduleCode = candidate.ScheduleCode,
                ExamTrialId = candidate.ExamTrialId,
                TrialNumber = candidate.TrialNumber,
                TotalExamDuration = candidate.PaperDuration,
                CandidateStartedExam = candidate.CandidateStartedExam,
                CandidateEndedExam = candidate.CandidateEndedExam,
                ExamTrialStartDate = candidate.ExamTrialStartDate,
                ExamTrialEndDate = candidate.ExamTrialEndDate,
                CandidateCurrentlyInExam = candidate.CandidateCurrentlyInExam,
                TotalQuestionsInDatabase = candidate.TotalQuestionsInDatabase,
                QuestionsAnsweredCount = candidate.QuestionsAnsweredCount,
                QuestionsMarkedForReview = candidate.QuestionsMarkedForReview,
                QuestionsVisited = candidate.QuestionsVisited,
                TotalElapsedTimeInSeconds = candidate.TotalElapsedTimeInSeconds,
                ReviewStatus = candidate.ReviewStatus,
                ReviewedAt = candidate.ReviewedAt,
                ReviewedBy = candidate.ReviewedBy,
                SentToCTR = candidate.SentToCTR,
                SentToCTRAt = candidate.SentToCTRAt,
                SentToCTRBy = candidate.SentToCTRBy,
                VenueCode = candidate.VenueCode,
                CandidateCreatedAt = candidate.CandidateCreatedAt,
                TrackingLogsJSON = candidate.TrackingLogsJSON
            };
        }

        public async Task<ApiResponse> GetAttendanceReportsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            if (startDate > endDate)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.StartDateCannotBeAfterEndDate
                );
            }

            var reports = await _commonService
                ._unitOfWork
                .Repository<PaperFormAttendanceReportView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(r => r.ExamDate >= startDate && r.ExamDate <= endDate)
                .OrderByDescending(r => r.PaperStartDate)
                .ToListAsync();

            if (reports.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoAttendanceReportsFoundForDateRange
                );
            }

            var reportDtos = reports.ConvertAll(r => MapToDto(r));

            var grouped = new AttendanceReportGroupedDto
            {
                ExamSeriesData = [.. reportDtos
                    .GroupBy(x => x.PaperCode)
                    .Select(g => new ExamSeriesGroupDto
                    {
                        ExamSeries = g.Key ?? string.Empty,
                        VenueCodeCount = g.Select(x => x.VenueDisplayName).Distinct().Count(),
                        Allocated = g.Sum(x => x.Allocated),
                        Attended = g.Sum(x => x.Attended),
                        Absent = g.Sum(x => x.Absent),
                        TakenSession = g.Sum(x => x.TakenSession),
                        UnfinishedSession = g.Sum(x => x.UnfinishedSession),
                        Reviewed = SumReviewed(g.Select(x => x.Reviewed)),
                        SentToCTR = g.Sum(x => x.Allocated) == 0 || g.Sum(x => x.Attended) == 0 || g.All(x => x.SentToCTR),
                        Children = [.. g.GroupBy(x => x.VenueDisplayName).Select(cg => new AllocationChildDto
                        {
                            Name = cg.Key ?? string.Empty,
                            Allocated = cg.Sum(x => x.Allocated),
                            Attended = cg.Sum(x => x.Attended),
                            Absent = cg.Sum(x => x.Absent),
                            TakenSession = cg.Sum(x => x.TakenSession),
                            UnfinishedSession = cg.Sum(x => x.UnfinishedSession),
                            Reviewed = SumReviewed(cg.Select(x => x.Reviewed)),
                            SentToCTR = cg.Sum(x => x.Allocated) == 0 || cg.Sum(x => x.Attended) == 0 || cg.All(x => x.SentToCTR)
                        })]
                    })],
                VenueCodeData = [.. reportDtos
                    .GroupBy(x => x.VenueDisplayName)
                    .Select(g => new VenueCodeGroupDto
                    {
                        VenueCode = g.First().VenueCode ?? string.Empty,
                        VenueDisplayName = g.Key ?? string.Empty,
                        Allocated = g.Sum(x => x.Allocated),
                        Attended = g.Sum(x => x.Attended),
                        Absent = g.Sum(x => x.Absent),
                        TakenSession = g.Sum(x => x.TakenSession),
                        UnfinishedSession = g.Sum(x => x.UnfinishedSession),
                        Reviewed = SumReviewed(g.Select(x => x.Reviewed)),
                        SentToCTR = g.Sum(x => x.Allocated) == 0 || g.Sum(x => x.Attended) == 0 || g.All(x => x.SentToCTR),
                        Children = [.. g.GroupBy(x => x.PaperCode).Select(cg => new AllocationChildDto
                        {
                            Name = cg.Key ?? string.Empty,
                            Allocated = cg.Sum(x => x.Allocated),
                            Attended = cg.Sum(x => x.Attended),
                            Absent = cg.Sum(x => x.Absent),
                            TakenSession = cg.Sum(x => x.TakenSession),
                            UnfinishedSession = cg.Sum(x => x.UnfinishedSession),
                            Reviewed = SumReviewed(cg.Select(x => x.Reviewed)),
                            SentToCTR = cg.Sum(x => x.Allocated) == 0 || cg.Sum(x => x.Attended) == 0 || cg.All(x => x.SentToCTR)
                        })]
                    })]
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.AttendanceReportsFoundForDateRange, reports.Count),
                grouped
            );
        }

        private static string SumReviewed(IEnumerable<string?> reviewedValues)
        {
            var parts = reviewedValues
                .Where(v => !string.IsNullOrWhiteSpace(v) && v.Contains('/'))
                .Select(v => v!.Split('/'))
                .Where(p => p.Length >= 2)
                .ToList();

            var reviewed = parts.Sum(p => int.TryParse(p[0].Trim(), out var r) ? r : 0);
            var total = parts.Sum(p => int.TryParse(p[1].Trim(), out var t) ? t : 0);

            return $"{reviewed}/{total}";
        }

        public async Task<ApiResponse> GetAllAttendanceReportsAsync()
        {
            var reports = await _commonService
                ._unitOfWork
                .Repository<PaperFormAttendanceReportView, long>()
                .GetAll()
                .AsNoTracking()
                .ToListAsync();

            if (reports == null || reports.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoAttendanceReportsFoundForDateRange
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.AttendanceReportsFoundForDateRange, reports.Count),
                reports.ConvertAll(MapToDto)
            );
        }

        public async Task<ApiResponse> GetAttendanceReportByPaperFormAsync(long paperFormId)
        {
            if (paperFormId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidPaperFormId
                );
            }

            var report = await _commonService
                ._unitOfWork
                .Repository<PaperFormAttendanceReportView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(r => r.PaperFormId == paperFormId)
                .FirstOrDefaultAsync();

            if (report == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoAttendanceReportFoundForPaperForm
                );
            }

            var reportDto = MapToDto(report);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.AttendanceReportRetrievedSuccessfully,
                reportDto
            );
        }

        public async Task<ApiResponse> GetAttendanceReportsByScheduleAsync(long scheduleId)
        {
            if (scheduleId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidScheduleId
                );
            }

            var reports = await _commonService
                ._unitOfWork
                .Repository<PaperFormAttendanceReportView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(r => r.ScheduleId == scheduleId)
                .OrderBy(r => r.ExamSeries)
                .ToListAsync();

            if (reports.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoAttendanceReportsFoundForSchedule
                );
            }

            var reportDtos = reports.ConvertAll(r => MapToDto(r));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.AttendanceReportsFoundForSchedule, reports.Count),
                reportDtos
            );
        }

        private static PaperFormAttendanceReportDto MapToDto(PaperFormAttendanceReportView report)
        {
            return new PaperFormAttendanceReportDto
            {
                PaperFormId = report.PaperFormId,
                ExamSeries = report.ExamSeries,
                Allocated = report.Allocated,
                Attended = report.Attended,
                Absent = report.Absent,
                TakenSession = report.TakenSession,
                UnfinishedSession = report.UnfinishedSession,
                Reviewed = report.Reviewed,
                AvgCompletionPercentage = report.AvgCompletionPercentage,
                AvgTimeSpentMinutes = report.AvgTimeSpentMinutes,
                PaperName = report.PaperName,
                PaperCode = report.PaperCode,
                PaperDuration = report.PaperDuration,
                PaperStartDate = report.PaperStartDate,
                PaperEndDate = report.PaperEndDate,
                ScheduleId = report.ScheduleId,
                ScheduleName = report.ScheduleName,
                ScheduleCode = report.ScheduleCode,
                VenueCode = report.VenueCode,
                VenueDisplayName = report.VenueDisplayName,
                NumSentToCTR = report.NumSentToCTR,
                SentToCTR = report.SentToCTR
            };
        }

        public async Task<ApiResponse> UpdateReviewStatusAsync(UpdateReviewStatusDto updateReviewStatusDto)
        {
            var candidateExamDetail = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .FirstOrDefaultAsync(x => x.Id == updateReviewStatusDto.Id);

            if (candidateExamDetail == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound
                );
            }

            // Update review status
            candidateExamDetail.ReviewStatus = updateReviewStatusDto.ReviewStatus;
            candidateExamDetail.ReviewedAt = DateTimeHelper.Now;
            candidateExamDetail.ReviewedBy = _filterParamsValues.UserEmail;

            _commonService._unitOfWork.Repository<CandidateExamDetails, long>().Update(candidateExamDetail);
            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ReviewStatusUpdatedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToUpdateReviewStatus
            );
        }

        public async Task<ApiResponse> ExportUnfinishedReviewAsync(ExportUnfinishedReviewRequestDto request)
        {
            var candidate = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (candidate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound
                );
            }

            var trackingLogs = await _commonService
                ._unitOfWork
                .Repository<CandidateTrackingLog, long>()
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.CandidateExamTrialId == request.CandidateExamTrialId &&
                    x.CandidateId == request.CandidateId &&
                    x.PaperId == request.PaperId &&
                    x.PaperFormId == request.PaperFormId &&
                    x.ScheduleId == request.ScheduleId)
                .Select(x => x.SessionLog)
                .ToListAsync();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var activityEntries = new List<TrackingLogEntryDto>();

            foreach (var sessionLog in trackingLogs)
            {
                if (string.IsNullOrWhiteSpace(sessionLog) || sessionLog == "{}")
                    continue;

                var decompressedLog = CompressionHelper.TryDecompressString(sessionLog);
                var entries = JsonSerializer.Deserialize<List<TrackingLogEntryDto>>(decompressedLog, jsonOptions);
                if (entries == null || entries.Count == 0) continue;

                entries = [.. entries.Where(e => e.Time.Date == entries[0].Time.Date)];

                activityEntries.AddRange(entries);
            }

            var visitedSectionsCount = activityEntries
                .Where(x => !string.IsNullOrWhiteSpace(x.SectionName))
                .Select(x => x.SectionName!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            int totalSections = 0;

            if (candidate.PaperType == PaperType.Adaptive)
            {
                totalSections = await _commonService._unitOfWork
                    .Repository<AdaptiveSection, long>()
                    .Query()
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.Stage.FormId == candidate.PaperFormIdActual)
                    .CountAsync();
            }
            else
            {
                totalSections = await _commonService._unitOfWork
                    .Repository<StandardSection, long>()
                    .Query()
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.FormId.HasValue && s.FormId.Value == candidate.PaperFormIdActual)
                    .CountAsync();
            }

            var skipped = candidate.TotalQuestionsInDatabase - candidate.QuestionsAnsweredCount;
            var examSeries = $"{candidate.PaperName} - {candidate.PaperFormName}";
            var examDate = (candidate.ExamTrialStartDate ?? candidate.CandidateExamDate).ToString("yyyy-MM-dd");
            var timeSpent = FormatExamTimeSpent(candidate.TotalElapsedTimeInSeconds);
            var sections = $"{visitedSectionsCount}/{totalSections}";
            var changedMachine = activityEntries
                .Where(x => !string.IsNullOrEmpty(x.IpAddress))
                .Select(x => x.IpAddress)
                .Distinct()
                .Count() > 1;

            var answerData = await _commonService._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(a => a.RegistrationId == candidate.RegistrationId && a.AnswerId != null)
                .Select(a => new { a.IsCorrect })
                .ToListAsync();

            var correctCount = answerData.Count(x => x.IsCorrect);
            var incorrectCount = answerData.Count(x => !x.IsCorrect);

            using var workbook = new XLWorkbook();

            // Single combined sheet — details (rows 1-6) then activity log (row 7+)
            var sheet = workbook.Worksheets.Add(Resource.CandidateStatusDetails);

            // Details: 3 groups of 5 columns, each group = 1 header row + 1 data row
            var detailGroups = new (string Label, string Value)[][]
            {
                [
                    (Resource.NationalId, candidate.CandidateNationalId ?? string.Empty),
                    (Resource.RegistrationId, candidate.CandidateCode ?? string.Empty),
                    (Resource.Name, candidate.CandidateDisplayName ?? string.Empty),
                    (Resource.ExamSeries, examSeries),
                    (Resource.DateOfExam, examDate),
                ],
                [
                    (Resource.TimeSpendInExamSession, timeSpent),
                    (Resource.NumberOfViewedQuestions, candidate.QuestionsVisited.ToString()),
                    (Resource.NumberOfCorrectAnswered, correctCount.ToString()),
                    (Resource.NumberOfIncorrectAnswered, incorrectCount.ToString()),
                    (Resource.NumberOfSkipped, skipped.ToString()),
                ],
                [
                    (Resource.ExamSessionsViewed, sections),
                    (Resource.TotalQuestions, candidate.TotalQuestionsInDatabase.ToString()),
                    (Resource.TotalExamDuration, $"{candidate.PaperDuration} min"),
                    (Resource.VenueCode, candidate.VenueCode ?? string.Empty),
                    (Resource.ChangedMachine, changedMachine ? Resource.Yes : Resource.No),
                ],
            };

            for (int groupIdx = 0; groupIdx < detailGroups.Length; groupIdx++)
            {
                int headerRow = groupIdx * 2 + 1; // 1, 3, 5
                int dataRow = headerRow + 1;    // 2, 4, 6
                var group = detailGroups[groupIdx];

                for (int col = 0; col < group.Length; col++)
                {
                    var headerCell = sheet.Cell(headerRow, col + 1);
                    headerCell.Value = group[col].Label;
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor = XLColor.LightGray;

                    sheet.Cell(dataRow, col + 1).Value = group[col].Value;
                }
            }

            // Activity Log — starts immediately at row 7 (right after the 6 detail rows)
            const int activityStartRow = 7;

            var orderedActivity = activityEntries.OrderBy(x => x.Time).ToList();

            var activityColumns = new List<ExcelExportHelper.ColumnDefinition<TrackingLogEntryDto>>
                {
                    new() { Header = Resource.Time, ValueSelector = x => x.Time.ToString("yyyy-MM-dd HH:mm:ss") },
                    new() { Header = Resource.Action, ValueSelector = x => x.Action.ToLocalizedString() },
                    new() { Header = Resource.LogMessage, ValueSelector = x => x.LogMessage ?? string.Empty },
                    new() { Header = Resource.Section, ValueSelector = x => x.SectionName ?? string.Empty },
                    new() { Header = Resource.QuestionId, ValueSelector = x => x.QuestionId },
                    new() { Header = Resource.IPAddress, ValueSelector = x => x.IpAddress ?? string.Empty },
                    new() { Header = Resource.QuestionStatus, ValueSelector = x => GetQuestionStatusLabel(x.QuestionStatus) },
                    new() { Header = Resource.Duration, ValueSelector = x => x.DurationInSeconds.HasValue? x.DurationInSeconds.Value >= 60? $"{x.DurationInSeconds.Value / 60} min {x.DurationInSeconds.Value % 60} sec": $"{x.DurationInSeconds.Value} sec": "-" },
                    new() { Header = Resource.ElementType, ValueSelector = x => x.ElementType ?? string.Empty },
                };

            // "#" header
            var indexHeader = sheet.Cell(activityStartRow, 1);
            indexHeader.Value = "#";
            indexHeader.Style.Font.Bold = true;
            indexHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < activityColumns.Count; i++)
            {
                var cell = sheet.Cell(activityStartRow, i + 2);
                cell.Value = activityColumns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            for (int rowIndex = 0; rowIndex < orderedActivity.Count; rowIndex++)
            {
                int excelRow = activityStartRow + 1 + rowIndex;
                sheet.Cell(excelRow, 1).Value = rowIndex + 1;

                for (int colIndex = 0; colIndex < activityColumns.Count; colIndex++)
                {
                    sheet.Cell(excelRow, colIndex + 2).Value =
                        activityColumns[colIndex].ValueSelector(orderedActivity[rowIndex])?.ToString();
                }
            }

            sheet.Columns().AdjustToContents();

            await using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var base64 = Convert.ToBase64String(stream.ToArray());

            var fileName = $"UnfinishedReview_{candidate.CandidateCode ?? candidate.Id.ToString()}_{DateTimeHelper.Now:yyyyMMdd_HHmmss}.xlsx";

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                new DownloadFileDto(fileName, base64)
            );
        }

        private static string FormatExamTimeSpent(double? seconds)
        {
            if (!seconds.HasValue || seconds.Value == 0)
                return "00:00";

            var timeSpan = TimeSpan.FromSeconds(seconds.Value);
            return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
        }

        private static string GetQuestionStatusLabel(int status) => status switch
        {
            0 => Resource.NotVisited,
            1 => Resource.Visited,
            2 => Resource.Answered,
            _ => string.Empty
        };

        public async Task<ApiResponse> AddCTRExamSyncJobsAsync(ExportCandidatesRequestDto request)
        {
            var jobStartTime = DateTimeHelper.Now;

            // Generate the export data by calling ExportCandidatesData
            var exportResponse = await _equationTemplateService.ExportCandidatesData(request);

            if (exportResponse.StatusCode != HttpStatusCode.OK || exportResponse.Data == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    exportResponse.Message ?? Resource.FailedToGenerateExportData
                );
            }

            var exportData = exportResponse.Data as ExportCandidatesResponseDto;

            if (string.IsNullOrEmpty(exportData.FileContent))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    Resource.InvalidExportDataStructure
                );
            }

            // Get folder path from configuration
            var ctrExamSettings = _configuration.GetSection(nameof(CTRExamSettings)).Get<CTRExamSettings>();
            var folderPath = ctrExamSettings.ExportFolderPath;

            if (string.IsNullOrEmpty(folderPath))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    Resource.CTRExportFolderPathNotConfigured
                );
            }

            // Ensure folder exists (supports both local and UNC network paths)
            var isUncPath = folderPath.StartsWith(@"\\");
            var fullPath = isUncPath ? folderPath : Path.Combine(Directory.GetCurrentDirectory(), folderPath);

            // Save zip file to folder
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(exportData.FileName);
            var rawFileName = $"{fileNameWithoutExt}.zip";
            var fileName = await ResolveUniqueFileNameAsync(rawFileName);
            var filePath = Path.Combine(fullPath, fileName);
            var zipBytes = Convert.FromBase64String(exportData.FileContent);
            if (isUncPath && !string.IsNullOrEmpty(ctrExamSettings.NetworkUsername))
            {
                using var connection = new OES.Services.Helpers.NetworkConnection(
                    fullPath,
                    ctrExamSettings.NetworkUsername,
                    ctrExamSettings.NetworkPassword,
                    ctrExamSettings.NetworkDomain ?? ""
                );
                connection.WriteFile(fileName, zipBytes);
            }
            else
            {
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                await File.WriteAllBytesAsync(filePath, zipBytes);
            }

            // Check if file was saved successfully
            if (!isUncPath && !File.Exists(filePath))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToSaveFileToCTRFolder
                );
            }

            // Check if sync job already exists for today
            var existingJob = await _commonService
                ._unitOfWork
                .Repository<CTRExamSyncJobs, long>()
                .GetAll()
                .FirstOrDefaultAsync(j =>
                    j.VenueId == request.VenueId &&
                    j.PaperFormId == request.FormId &&
                    j.IsSuccess &&
                    j.GenerateResultStartDate == DateOnly.FromDateTime(request.StartDate.Value) &&
                    j.GenerateResultEndDate == DateOnly.FromDateTime(request.EndDate.Value)
                );

            CTRExamSyncJobs syncJob;

            // Create or update sync job
            if (existingJob == null)
            {
                syncJob = new CTRExamSyncJobs
                {
                    VenueId = request.VenueId,
                    VenueCode = exportData.VenueCode,
                    PaperId = exportData.PaperId,
                    PaperFormId = request.FormId,
                    TotalCandidates = exportData.CandidateCount,
                    FileName = fileName,
                    GenerateResultStartDate = request.StartDate.HasValue
                        ? DateOnly.FromDateTime(request.StartDate.Value)
                        : null,
                    GenerateResultEndDate = request.EndDate.HasValue
                        ? DateOnly.FromDateTime(request.EndDate.Value)
                        : null,
                    JobStartTime = jobStartTime,
                    JobEndTime = DateTimeHelper.Now,
                    IsSuccess = true,
                    ErrorCode = null
                };

                await _commonService._unitOfWork.Repository<CTRExamSyncJobs, long>().AddAsync(syncJob);
            }
            else
            {
                syncJob = existingJob;
                syncJob.VenueCode = exportData.VenueCode;
                syncJob.TotalCandidates = exportData.CandidateCount;
                syncJob.FileName = fileName;
                syncJob.PaperId = exportData.PaperId;
                syncJob.GenerateResultStartDate = request.StartDate.HasValue
                    ? DateOnly.FromDateTime(request.StartDate.Value)
                    : null;
                syncJob.GenerateResultEndDate = request.EndDate.HasValue
                    ? DateOnly.FromDateTime(request.EndDate.Value)
                    : null;
                syncJob.JobStartTime = jobStartTime;
                syncJob.JobEndTime = DateTimeHelper.Now;
                syncJob.IsSuccess = true;
                syncJob.ErrorCode = null;

                _commonService._unitOfWork.Repository<CTRExamSyncJobs, long>().Update(syncJob);
            }

            await _commonService._unitOfWork.Complete();

            // Update CandidateExamDetails after successful sync
            if (syncJob.IsSuccess)
            {
                var candidateDetails = await _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .GetAll()
                    .Where(v => v.PaperFormIdActual == request.FormId && v.VenueCode == exportData.VenueCode)
                    .ToListAsync();

                if (candidateDetails.Count > 0)
                {
                    var now = DateTimeHelper.Now;

                    foreach (var candidate in candidateDetails)
                    {
                        candidate.SentToCTR = true;
                        candidate.SentToCTRAt = now;
                        candidate.SentToCTRBy = _filterParamsValues.UserEmail;
                        candidate.JobId = syncJob.Id;
                    }

                    _commonService
                        ._unitOfWork
                        .Repository<CandidateExamDetails, long>()
                        .UpdateRange(candidateDetails);

                    await _commonService._unitOfWork.Complete();
                }
            }

            await _commonService._unitOfWork.Repository<CTRResultsFileName, long>().AddAsync(new CTRResultsFileName
            {
                FileName = fileName,
                SentAt = DateTimeHelper.Now,
                SentBy = _filterParamsValues.UserEmail
            });

            await _commonService._unitOfWork.Complete();

            // Return success response
            var fileInfo = new FileInfo(filePath);
            var duration = (syncJob.JobEndTime.Value - syncJob.JobStartTime.Value).TotalSeconds;

            return new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = Resource.FileGeneratedAndSavedSuccessfully,
                Data = new
                {
                    SyncJobId = syncJob.Id,
                    FilePath = filePath,
                    FileName = syncJob.FileName,
                    FileSize = fileInfo.Length,
                    CandidateCount = syncJob.TotalCandidates,
                    Duration = duration,
                    IsSuccess = true
                }
            };
        }

        public async Task<ApiResponse> GetCTRExamSyncJobsAsync(ExportCandidatesRequestDto request)
        {
            var existingJob = await _commonService
                ._unitOfWork
                .Repository<CTRExamSyncJobs, long>()
                .GetAll()
                .Where(j =>
                   j.VenueId == request.VenueId &&
                   j.VenueCode == request.VenueCode &&
                   j.PaperFormId == request.FormId &&
                   j.IsSuccess &&
                   j.FileName != null &&
                   j.JobStartTime != null
                ).ToListAsync();

            if (existingJob == null || existingJob.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCTRExamSyncJobsFound
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CTRExamSyncJobsRetrievedSuccessfully,
                existingJob
            );
        }

        public async Task<ApiResponse> SaveCombinedFileToCTRAsync(SaveFileToCTRDto dto)
        {
            try
            {
                var ctrExamSettings = _configuration.GetSection(nameof(CTRExamSettings)).Get<CTRExamSettings>();
                var folderPath = ctrExamSettings.ExportFolderPath;

                var isUncPath = folderPath.StartsWith(@"\\");
                var fullPath = isUncPath ? folderPath : Path.Combine(Directory.GetCurrentDirectory(), folderPath);
                var fileBytes = Convert.FromBase64String(dto.Base64Content);
                dto.FileName = await ResolveUniqueFileNameAsync(dto.FileName);
                var filePath = Path.Combine(fullPath, dto.FileName);

                if (isUncPath && !string.IsNullOrEmpty(ctrExamSettings.NetworkUsername))
                {
                    using var connection = new Helpers.NetworkConnection(
                        fullPath,
                        ctrExamSettings.NetworkUsername,
                        ctrExamSettings.NetworkPassword,
                        ctrExamSettings.NetworkDomain ?? ""
                    );
                    connection.WriteFile(dto.FileName, fileBytes);
                }
                else
                {
                    if (!Directory.Exists(fullPath))
                        Directory.CreateDirectory(fullPath);
                    await File.WriteAllBytesAsync(filePath, fileBytes);
                }

                await _commonService._unitOfWork.Repository<CTRResultsFileName, long>().AddAsync(new CTRResultsFileName
                {
                    FileName = dto.FileName,
                    SentAt = DateTimeHelper.Now,
                    SentBy = _filterParamsValues.UserEmail
                });

                await _commonService._unitOfWork.Complete();

                if (dto.Combinations?.Count > 0)
                {
                    var now = DateTimeHelper.Now;

                    var updatedCount = await _commonService._unitOfWork
                        .Repository<CandidateExamDetails, long>()
                        .Query(applySignature: false, applyOrganizationIdFilter: false)
                        .Where(v => dto.RegistrationIds.Contains(v.RegistrationId))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(v => v.SentToCTR, true)
                            .SetProperty(v => v.SentToCTRAt, now)
                            .SetProperty(v => v.SentToCTRBy, _filterParamsValues.UserEmail)
                        );
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.FileGeneratedAndSavedSuccessfully
                );
            }
            catch (Exception ex)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    ex.Message
                );
            }
        }

        public async Task<ApiResponse> MarkExportsAsSentToCTRAsync(List<ExportCandidatesRequestDto> requests)
        {
            var now = DateTimeHelper.Now;

            foreach (var request in requests)
            {
                var candidates = await _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .GetAll()
                    .Where(v => v.PaperFormIdActual == request.FormId && v.VenueCode == request.VenueCode &&
                                (v.ReviewStatus == ReviewStatus.NotApplicable || v.ReviewStatus == ReviewStatus.Accepted))
                    .ToListAsync();
                // TODO: Check all answers, they should have EvaluationStatus == Completed or NotApplicable (If there is one answer, else don't send this data to CTR)
                // TODO: We need to taka a list of candidates RegistrationIds to make sure we are updating the correct candidates exams.

                foreach (var candidate in candidates)
                {
                    candidate.SentToCTR = true;
                    candidate.SentToCTRAt = now;
                    candidate.SentToCTRBy = _filterParamsValues.UserEmail;
                }

                _commonService._unitOfWork.Repository<CandidateExamDetails, long>().UpdateRange(candidates);
            }

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Marked as sent to CTR successfully"
            );
        }

        public async Task<ApiResponse> GetAllCTRExamSyncJobsForDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            var syncJobs = await _commonService
                ._unitOfWork
                .Repository<CTRExamSyncJobs, long>()
                .GetAll()
                .AsNoTracking()
                .Where(j =>
                    j.IsSuccess &&
                    j.FileName != null &&
                    j.JobStartTime != null &&
                    j.GenerateResultStartDate >= startDate &&
                    j.GenerateResultEndDate <= endDate
                )
                .Select(j => new CTRExamSyncJobDto
                {
                    Id = j.Id,
                    VenueId = j.VenueId,
                    VenueCode = j.VenueCode,
                    PaperId = j.PaperId,
                    PaperFormId = j.PaperFormId,
                    TotalCandidates = j.TotalCandidates,
                    GenerateResultStartDate = j.GenerateResultStartDate,
                    GenerateResultEndDate = j.GenerateResultEndDate,
                    FileName = j.FileName,
                    JobStartTime = j.JobStartTime,
                    JobEndTime = j.JobEndTime,
                    IsSuccess = j.IsSuccess,
                    ErrorCode = j.ErrorCode
                })
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CTRExamSyncJobsRetrievedSuccessfully,
                syncJobs
            );
        }

        public async Task<ApiResponse> AddBlockCandidateAnswersAsync(List<AddBlockCandidateAnswerDto> blockCandidateAnswerDtos)
        {
            if (blockCandidateAnswerDtos == null || blockCandidateAnswerDtos.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.BlockCandidateAnswersListCannotBeEmpty
                );
            }

            // Extract distinct keys from incoming DTOs for targeted DB query
            var candidateIds = blockCandidateAnswerDtos.Select(dto => dto.CandidateId).Distinct().ToList();
            var registrationNumbers = blockCandidateAnswerDtos.Select(dto => dto.RegistrationNumber).Distinct().ToList();

            // Query ONLY potential duplicates — not the full table
            var existingAnswers = await _commonService
                ._unitOfWork
                .Repository<BlockCandidateAnswer, long>()
                .Query()
                .AsNoTracking()
                .Where(x => candidateIds.Contains(x.CandidateId) && registrationNumbers.Contains(x.RegistrationNumber))
                .Select(x => new
                {
                    x.CandidateId,
                    x.RegistrationNumber,
                    x.BlockId,
                    x.PaperId,
                    x.PaperFormId
                })
                .ToListAsync();

            // Use HashSet for O(1) duplicate lookups
            var existingSet = existingAnswers
                .Select(x => (x.CandidateId, x.RegistrationNumber, x.BlockId, x.PaperId, x.PaperFormId))
                .ToHashSet();

            // Filter out duplicates - keep only new records
            var newAnswers = blockCandidateAnswerDtos
                .Where(dto => !existingSet.Contains((dto.CandidateId, dto.RegistrationNumber, dto.BlockId, dto.PaperId, dto.PaperFormId)))
                .ToList();

            // If all records already exist, return success with count 0
            if (newAnswers.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.AllBlockCandidateAnswersAlreadyExistInDatabase,
                    0
                );
            }

            // Convert DTOs to entities
            var blockCandidateAnswers = newAnswers.ConvertAll(dto => new BlockCandidateAnswer
            {
                CandidateId = dto.CandidateId,
                RegistrationNumber = dto.RegistrationNumber,
                PaperId = dto.PaperId,
                PaperFormId = dto.PaperFormId,
                BlockId = dto.BlockId,
                CandidateExamTrialId = dto.CandidateExamTrialId,
                ScheduleId = dto.ScheduleId,
                SchedulePaperId = dto.SchedulePaperId,
                TotalScore = dto.TotalScore,
                TotalQuestionsCount = dto.TotalQuestionsCount,
                CorrectQuestionsCount = dto.CorrectQuestionsCount,
                IncorrectQuestionsCount = dto.IncorrectQuestionsCount,
                Synced = dto.Synced,
                SyncDate = dto.SyncDate,
                CreatedAt = dto.CreatedAt ?? DateTimeHelper.Now,
                CreatedBy = nameof(System),
                IsActive = true,
                IsDeleted = false,
                OrganizationId = dto.OrganizationId,
                OrganizationSignature = string.IsNullOrWhiteSpace(dto.OrganizationSignature)
                    ? MiscConstants.OrganizationSignature
                    : dto.OrganizationSignature,
                VenueCode = dto.VenueCode,
                VenueId = dto.VenueId
            });

            // Save only new records
            _commonService._unitOfWork.Repository<BlockCandidateAnswer, long>().AddRangAsync(blockCandidateAnswers);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                int duplicateCount = blockCandidateAnswerDtos.Count - newAnswers.Count;

                string message = duplicateCount > 0
                    ? string.Format(Resource.NewBlockCandidateAnswersAddedDuplicatesSkipped, newAnswers.Count, duplicateCount)
                    : string.Format(Resource.BlockCandidateAnswersAddedSuccessfully, newAnswers.Count);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    message,
                    newAnswers.Count
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToAddBlockCandidateAnswers
            );
        }

        public async Task<ApiResponse> GetSyncStatusOESToCESReportAsync()
        {
            var reports = await _commonService
                ._unitOfWork
                .Repository<SyncStatusOESToCESReportView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(r => r.Status == SyncJobStatus.Success)
                .ToListAsync();

            if (reports == null || reports.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoSyncStatusReportFound
                );
            }

            var reportDtos = reports.ConvertAll(report => new SyncStatusOESToCESReportDto
            {
                JobCount = report.JobCount,
                VenueId = report.VenueId,
                VenueName = report.VenueName,
                Status = report.Status,
                SyncDate = report.SyncDate,
                Duration = report.Duration,
                LatestRunDate = report.LatestRunDate
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SyncStatusReportRetrievedSuccessfully,
                reportDtos
            );
        }

        public async Task<ApiResponse> GetSyncStatusCESToOESReportAsync()
        {
            var reports = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .GetAll()
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.VenueCode))
                .GroupBy(c => c.VenueCode)
                .Select(g => new
                {
                    VenueCode = g.Key,
                    CandidateCount = g.Count(),
                    LatestRun = g.Max(c => c.CreationDate),
                    MinCreationDate = g.Min(c => c.CreationDate),
                    MaxCreationDate = g.Max(c => c.CreationDate)
                })
                .ToListAsync();

            if (reports == null || reports.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoSyncStatusReportFound
                );
            }

            var reportDtos = reports.ConvertAll(report =>
            {
                var durationInSeconds = (int)(report.MaxCreationDate - report.MinCreationDate).TotalSeconds;
                var status = report.LatestRun.Date == DateTimeHelper.Now.Date ? SyncStatusReport.Running : SyncStatusReport.Done;

                return new SyncStatusCESToOESReportDto
                {
                    CandidateCount = report.CandidateCount,
                    VenueCode = report.VenueCode,
                    Status = status,
                    Duration = durationInSeconds,
                    LatestRunTime = report.LatestRun
                };
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SyncStatusReportRetrievedSuccessfully,
                reportDtos
            );
        }

        public async Task<ApiResponse> AddTrackingLogsAsync(List<AddTrackingLogDto> trackingLogDtos)
        {
            if (trackingLogDtos == null || trackingLogDtos.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.TrackingLogsListCannotBeEmpty
                );
            }

            // Extract distinct keys from incoming DTOs for targeted DB query
            var candidateIds = trackingLogDtos.Select(dto => dto.CandidateId).Distinct().ToList();
            var examTrialIds = trackingLogDtos.Select(dto => dto.CandidateExamTrialId).Distinct().ToList();

            // Query ONLY potential duplicates — not the full table
            var existingLogs = await _commonService
                ._unitOfWork
                .Repository<CandidateTrackingLog, long>()
                .Query()
                .AsNoTracking()
                .Where(x => candidateIds.Contains(x.CandidateId) && examTrialIds.Contains(x.CandidateExamTrialId))
                .Select(x => new { x.CandidateId, x.ScheduleId, x.CandidateExamTrialId, x.VenueCode })
                .ToListAsync();

            // Use HashSet for O(1) duplicate lookups
            var existingSet = existingLogs
                .Select(x => (x.CandidateId, x.ScheduleId, x.CandidateExamTrialId, x.VenueCode))
                .ToHashSet();

            // Filter out duplicates - keep only new records
            var newLogs = trackingLogDtos
                .Where(dto => !existingSet.Contains((dto.CandidateId, dto.ScheduleId, dto.CandidateExamTrialId, dto.VenueCode)))
                .ToList();

            if (newLogs.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.AllTrackingLogsAlreadyExistInDatabase,
                    0
                );
            }

            var entities = newLogs.ConvertAll(dto => new CandidateTrackingLog
            {
                CandidateId = dto.CandidateId,
                PaperId = dto.PaperId,
                PaperFormId = dto.PaperFormId,
                ScheduleId = dto.ScheduleId,
                CandidateExamTrialId = dto.CandidateExamTrialId,
                SessionLog = dto.SessionLog,
                VenueCode = dto.VenueCode
            });

            _commonService._unitOfWork.Repository<CandidateTrackingLog, long>().AddRangAsync(entities);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                int duplicateCount = trackingLogDtos.Count - newLogs.Count;

                string message = duplicateCount > 0
                     ? string.Format(Resource.TrackingLogsAddedWithDuplicates, newLogs.Count, duplicateCount)
                     : string.Format(Resource.TrackingLogsAddedSuccessfully, newLogs.Count);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    message,
                    newLogs.Count
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToAddTrackingLogs
            );
        }

        public async Task<ApiResponse> GetTrackingLogsByRequestAsync(GetTrackingLogRequestDto request)
        {
            var logs = await _commonService
                ._unitOfWork
                .Repository<CandidateTrackingLog, long>()
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.CandidateExamTrialId == request.CandidateExamTrialId &&
                    x.CandidateId == request.CandidateId &&
                    x.PaperId == request.PaperId &&
                    x.PaperFormId == request.PaperFormId &&
                    x.ScheduleId == request.ScheduleId)
                .Select(x => new GetTrackingLogDto
                {
                    Id = x.Id,
                    CandidateExamTrialId = x.CandidateExamTrialId,
                    SessionLog = x.SessionLog,
                    VenueCode = x.VenueCode
                })
                .ToListAsync();

            foreach (var log in logs)
                log.SessionLog = CompressionHelper.TryDecompressString(log.SessionLog);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Empty,
                logs
            );
        }

        public async Task<ApiResponse> UpdateEvaluationScoresAsync(List<CandidateAnswersScoresDto> scores)
        {
            if (scores == null || scores.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ScoresListCannotBeEmpty
                );
            }

            var ids = scores.ConvertAll(s => s.Id);
            var candidateQuestionsAnswers = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            if (candidateQuestionsAnswers.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoRecordsFoundToUpdate
                );
            }

            foreach (var score in scores)
            {
                var candidateAnswer = candidateQuestionsAnswers.FirstOrDefault(x => x.Id == score.Id);
                if (candidateAnswer != null)
                {
                    candidateAnswer.MarksObtained = score.MarksObtained;
                    candidateAnswer.IsCorrect = score.IsCorrect;
                    candidateAnswer.EvaluationStatus = EvaluationStatus.EvaluationCompleted;
                    candidateAnswer.EvaluationSyncDate = DateTimeHelper.Now;
                }
            }

            _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .UpdateRange(candidateQuestionsAnswers);

            var rowsAffected = await _commonService
                ._unitOfWork
                .Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.EvaluationScoresUpdatedSuccessfully, rowsAffected),
                rowsAffected
            );
        }

        #region Helper Methods

        private async Task<string> ResolveUniqueFileNameAsync(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);

            var existingNames = await _commonService
                ._unitOfWork
                .Repository<CTRResultsFileName, long>()
                .GetAll()
                .AsNoTracking()
                .Where(x => x.FileName.StartsWith(nameWithoutExt))
                .Select(x => x.FileName)
                .ToListAsync();

            if (!existingNames.Contains(fileName))
                return fileName;

            int counter = 1;
            string newName;

            do
            {
                newName = $"{nameWithoutExt}_p{counter}{ext}";
                counter++;
            }
            while (existingNames.Contains(newName));

            return newName;
        }

        #endregion
    }
}