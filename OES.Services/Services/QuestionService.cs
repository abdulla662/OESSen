using AutoMapper;
using ClosedXML.Excel;
using HtmlAgilityPack;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OES.Core.DatabaseObjects.Procedures.Questions;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Core.Entities.QuestionQualityCheck;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.EmailQuestionAnswerDto;
using OES.Helper.Dtos.FileDetails;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.FillBlankAnswerDto;
using OES.Helper.Dtos.HotSpotDtos;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.CountsOfQuestionTypes;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.Navigation;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionIndicators;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionTemplateDto;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.WebRegistrationQuestionDtos;
using OES.Helper.Dtos.Question.WebSearchQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.QuestionIndicator;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.Helpers;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using QuestionTypeEnum = OES.Helper.Enums.QuestionType;
using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Services.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionValidatorService _questionValidatorService;
        private readonly IQuestionHtmlHelperService _htmlHelperService;
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IAutoPermissionAssignmentService _autoPermissionAssignmentService;
        private readonly IItemBankAuthorizationService _itemBankAuthorizationService;
        private readonly IStringLocalizer _localizer;
        private readonly IMemoryCache _memCache;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public QuestionService(IQuestionValidatorService questionValidatorService,
                               ICommonService commonService,
                               IMapper mapper,
                               FilterParamsValues filterParamsValues,
                               IQuestionHtmlHelperService htmlHelperService,
                               IAutoPermissionAssignmentService autoPermissionAssignmentService,
                               IItemBankAuthorizationService itemBankAuthorizationService,
                               IStringLocalizer<QuestionService> localizer,
                               IMemoryCache memCache
        )
        {
            _questionValidatorService = questionValidatorService;
            _commonService = commonService;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
            _htmlHelperService = htmlHelperService;
            _autoPermissionAssignmentService = autoPermissionAssignmentService;
            _itemBankAuthorizationService = itemBankAuthorizationService;
            _localizer = localizer;
            _memCache = memCache;
        }

        public async Task<ApiResponse> ValidateStandaloneQuestionsAsync(UploadFreeQuestions request)
        {
            if (request == null || request.File == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploaded
                );
            }

            var processor = new StandaloneQuestionFileProcessingService(_commonService);

            var result = await processor.ProcessFileAsync(request.File);

            return result;
        }

        public async Task<ApiResponse> GetStandaloneQuestionMetadataAsync(PaginationSearchModel paginationSearchModel)
        {
            if (paginationSearchModel == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.PaginationSearchModelCannotBeNull
                );
            }

            var spSearchKey = (paginationSearchModel.SearchInName && !string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
                ? paginationSearchModel.SearchKey
                : string.Empty;

            var spOrderBy = paginationSearchModel.OrderBy == SearchInKey.DESC ? "DESC" : "ASC";
            var spPageIndex = Math.Max(0, paginationSearchModel.PageIndex);
            var spPageSize = paginationSearchModel.PaginationOff ? int.MaxValue : Math.Max(1, paginationSearchModel.PageSize);

            var parameters = new List<(string Name, object Value)>
            {
                ("p_SearchKey", spSearchKey),
                ("p_OrderBy", spOrderBy),
                ("p_PageIndex", spPageIndex),
                ("p_PageSize", spPageSize),
            };

            var results = await _commonService._unitOfWork.ExecuteStoredProcedureAsync<StandaloneQuestionsResponseDto>(
                "sp_GetStandaloneQuestionMetadata",
                parameters,
                CancellationToken.None
            ).ConfigureAwait(false);

            var list = results?.ToList() ?? [];

            var totalRecords = list.Count != 0 ? list[0].TotalRecords : 0;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.StandaloneQuestions,
                new CustomTableData<StandaloneQuestionsResponseDto>(list, totalRecords)
            );
        }

        public async Task<ApiResponse> GetAllQuestionLanguage(PaginationSearchModel pagination, long QuestionMetaDataId = 0)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAll(x => x.QuestionMetadataId == QuestionMetaDataId, Including: "Language,QuestionsChoices")
                .AsNoTracking();

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
                {
                    query = query.Where(x => x.Body.Contains(pagination.SearchKey));
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                             o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = _mapper.Map<List<QuestionLanguageDTO>>(data);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null,
                                                                  new CustomTableData<QuestionLanguageDTO>(mappedData, totalItems));
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                var mappedData = _mapper.Map<List<QuestionLanguageDTO>>(data);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null,
                                                                  new CustomTableData<QuestionLanguageDTO>(mappedData, data.Count));
            }
        }

        public async Task<ApiResponse> GetAllQuestionAsync(PaginationSearchModel paginationSearchModel)
        {
            if (paginationSearchModel == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.PaginationSearchModelCannotBeNull
                );
            }

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => (x.ParentId == null || x.ParentId == 0) && x.IsRoot, null)
                .Include(i => i.QuestionDetails)
                .ThenInclude(x => x.Language)
                .Include(i => i.QuestionType)
                .Include(i => i.QuestionCategory)
                .Include(i => i.QuestionGroups)
                .Include(i => i.ItemBank)
                .ThenInclude(i => i.ItemBankGroups)
                .Include(i => i.Subject)
                .AsQueryable();

            var isAdmin = _filterParamsValues
                .SsoUserRoles
                .Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isAdmin)
            {
                var userGroupIds = _filterParamsValues
                    .OesUserGroupsAndRoles
                    .Select(g => g.GroupId)
                    .ToHashSet();

                query = query.Where(q =>
                    (q.QuestionGroups.Any() &&
                    q.QuestionGroups.Any(g => userGroupIds.Contains(g.OESGroupId) &&
                    !g.IsDeleted)) ||
                    (q.ItemBank != null && q.ItemBank.ItemBankGroups.Any(ibg => userGroupIds.Contains(ibg.OESGroupId) &&
                    !ibg.IsDeleted))
                );
            }

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => a.Code.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInBody)
            {
                query = query.Where(a => a.QuestionDetails.FirstOrDefault().Body.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<QuestionFilterPaginationModel>();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter._selectedItemBankSignature))
                    {
                        if (filter._selectedBranchId > 0)
                        {
                            query = query.Where(q => q.ItemBankId == filter._selectedBranchId);
                        }
                        else
                        {
                            query = query.Where(q => q.ItemBank.ItemBankSignature == filter._selectedItemBankSignature);
                        }
                    }
                    else if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }
                    if (filter._selectedCategory > 0)
                    {
                        query = query.Where(q => q.QuestionCategoryId == filter._selectedCategory);
                    }
                    if (filter._selectedType > 0)
                    {
                        query = query.Where(q => q.QuestionType.Id == filter._selectedType);
                    }
                    if (filter._selectedStatus > 0)
                    {
                        query = query.Where(q => q.QuestionStatus == filter._selectedStatus);
                    }
                    if (filter._selectedDifficultyLevel > 0)
                    {
                        query = query.Where(q => q.DifficultyLevelId == filter._selectedDifficultyLevel);
                    }
                    if (filter._selectedDeltaType > 0)
                    {
                        query = query.Where(q => q.DifficultyLevel.DeltaTypeId == filter._selectedDeltaType);
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No question found!");
            }

            var paginatedQuestions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (!paginatedQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No more questions found!");
            }

            var questionsDto = _mapper.Map<List<QuestionMetadataPaginationDto>>(paginatedQuestions);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "Questions!", new CustomTableData<QuestionMetadataPaginationDto>(questionsDto.ToList(), totalRecords));
        }

        public async Task<ApiResponse> GetAllQuestionByBlockId(long blockId, PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<BlockQuestion, long>()
                .Query()
                .Where(x => x.BlockId == blockId &&
                            x.QuestionMetadata.IsRoot &&
                            (x.QuestionMetadata.ParentId == null || x.QuestionMetadata.ParentId == 0) &&
                            x.QuestionMetadata.QuestionStatus == QuestionStatus.Approved)
                .Include(x => x.Block)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.QuestionDetails)
                        .ThenInclude(qd => qd.Language)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.QuestionType)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.QuestionCategory)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.ItemBank)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.Subject)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.DifficultyProfile)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.DifficultyLevel)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKey = paginationSearchModel.SearchKey.ToLower();
                query = query.Where(a =>
                    (a.QuestionMetadata.Code != null && a.QuestionMetadata.Code.ToLower().Contains(searchKey)) || a.QuestionMetadata.QuestionDetails.Any(qd => qd.Body.ToLower().Contains(searchKey)));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No question found!");
            }

            var questionsDto = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .Select(x => new BlockQuestionsDetailsPaginationDto
                {
                    Id = x.QuestionMetadata.Id,
                    Code = x.QuestionMetadata.Code,
                    Subject = x.QuestionMetadata.Subject.Name,
                    Type = x.QuestionMetadata.QuestionType.Name,
                    Category = x.QuestionMetadata.QuestionCategory.Name,
                    ItemBank = x.QuestionMetadata.ItemBank.Name,
                    DifficultyProfile = x.QuestionMetadata.DifficultyProfile.Name,
                    DifficultyLevel = x.QuestionMetadata.DifficultyLevel.Name,
                    Language = string.Join(", ",
                        x.QuestionMetadata.QuestionDetails
                            .Where(qd => qd.Language != null)
                            .Select(qd => qd.Language.Name))
                })
                .ToListAsync();

            if (questionsDto.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No more questions found!");
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Questions!",
                new CustomTableData<BlockQuestionsDetailsPaginationDto>(questionsDto, totalRecords)
            );
        }

        public async Task<ApiResponse> GetAllQuestionFromItemBankPoint(long paperId, PaginationSearchModel paginationSearchModel)
        {
            var targetPaper = await _commonService._unitOfWork.Repository<PaperMetadata, long>().GetObjAsync(x => x.Id == paperId);

            if (targetPaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperNotFound);
            }

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.QuestionStatus == QuestionStatus.Approved &&
                            x.IsRoot &&
                            x.ParentId == null &&
                            x.CurrentExhaustionCount < x.QuestionsExhaustionCount &&
                            x.ItemBank.ItemBankPoints.Any(p => p.PaperId == paperId) &&
                            x.DifficultyProfileId == targetPaper.DifficultyProfileId &&
                            x.QuestionDetails.Any(z => z.LanguageId == targetPaper.LanguageId) &&
                            (!targetPaper.AllowInstantResult || x.QuestionType.IsAutoCorrectable))
                .Include(i => i.QuestionDetails)
                .ThenInclude(x => x.Language)
                .Include(i => i.QuestionType)
                .Include(i => i.DifficultyLevel)
                .Include(i => i.ItemBank)
                .ThenInclude(i => i.ItemBankPoints.Where(p => p.PaperId == paperId))
                .Include(x => x.FormQuestions.Where(p => p.Form.PaperId == paperId))
                .Include(x => x.SubQuestions)
                .AsQueryable();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => a.Code.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInBody)
            {
                query = query.Where(a => a.QuestionDetails.FirstOrDefault().Body.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<QuestionFilterPaginationModel>();

                if (filter != null)
                {
                    if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }
                    if (filter._selectedType > 0)
                    {
                        query = query.Where(q => q.QuestionType.Id == filter._selectedType);
                    }
                    if (filter._selectedDifficultyLevel > 0)
                    {
                        query = query.Where(q => q.DifficultyLevelId == filter._selectedDifficultyLevel);
                    }
                    if (filter._toDelta != 0 || filter._fromDelta != 0)
                    {
                        query = query.Where(q =>
                            (q.Delta > 0 && q.Delta >= filter._fromDelta && q.Delta <= filter._toDelta) ||
                            ((q.Delta == 0 && ((filter._fromDelta >= q.DifficultyLevel.FromDelta && filter._fromDelta <= q.DifficultyLevel.ToDelta) ||
                             (filter._toDelta >= q.DifficultyLevel.FromDelta && filter._toDelta <= q.DifficultyLevel.ToDelta))) ||
                             (q.DifficultyLevel.FromDelta >= filter._fromDelta && q.DifficultyLevel.ToDelta <= filter._toDelta)));
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.CreationDate).ThenBy(x => x.Id);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoQuestionsFound);
            }

            var paginatedQuestions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (paginatedQuestions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoQuestionsFound);
            }

            var mappedQuestionsDtos = new List<ManualQuestionsPaginationResponseDto>();

            paginatedQuestions.ForEach(questionMetadata =>
            {
                mappedQuestionsDtos.Add(new ManualQuestionsPaginationResponseDto(
                    questionMetadata.Id,
                    questionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == targetPaper.LanguageId).Body,
                    questionMetadata.Code,
                    questionMetadata.QuestionType.Name,
                    questionMetadata.ItemBank.ItemBankPoints.FirstOrDefault(p => p.PaperId == paperId).Id,
                    questionMetadata.ItemBank.Id,
                    questionMetadata.ItemBank.Name,
                    questionMetadata.DifficultyLevelId,
                    questionMetadata.DifficultyLevel.Name,
                    questionMetadata.FormQuestions.Count,
                    questionMetadata.QuestionTypeId != (int)Helper.Enums.QuestionType.Comprehension ? 1 : questionMetadata.SubQuestions.Count
                ));
            });

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, new CustomTableData<ManualQuestionsPaginationResponseDto>(mappedQuestionsDtos, totalRecords));
        }

        public async Task<ApiResponse> GetAllQuestionForPendingPage(PaginationSearchModel paginationSearchModel)
        {
            if (!Guid.TryParse(_filterParamsValues.UserId, out Guid currentUserId))
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.InternalServerError, HttpStatusCode.Unauthorized, Resource.UserNotFound);
            }

            var committeeItemBankRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeItemBank, long>();

            var userItemBankIdsQuery = committeeItemBankRepo
                .GetAll(ci => ci.Committee.Members.Any(m => m.UserId == currentUserId))
                .Select(ci => ci.ItemBankId)
                .Distinct();

            var itemBankGroupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

            var groupItemBankIdsQuery = itemBankGroupRepo
              .GetAll(ibg =>
                  ibg.OESGroup.AppUserProfileGroups.Any(aug => aug.AppUserProfileId == currentUserId) &&
                  ibg.OESGroup.GroupResources.Any(gr =>
                      gr.ResourceRoles.Any(rr => rr.Role.Name == OesTemplateRoleConstants.QuestionQualityChecker)
                  )
              )
              .Select(ibg => ibg.ItemBankId)
              .Distinct();

            var mergedItemBankIds = userItemBankIdsQuery.Union(groupItemBankIdsQuery);

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => x.IsRoot && (x.ParentId == null || x.ParentId == 0) && x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending,
                        null,
                        $"{nameof(QuestionMetadata.QuestionDetails)}.{nameof(Language)}," +
                        $"{nameof(QuestionMetadata.QuestionType)}," +
                        $"{nameof(QuestionMetadata.QuestionCategory)}," +
                        $"{nameof(QuestionMetadata.ItemBank)}," +
                        $"{nameof(QuestionMetadata.Subject)}"
                );

            var isSuperOrEntityAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isSuperOrEntityAdmin)
            {
                query = query.Where(q => mergedItemBankIds.Contains(q.ItemBankId));
            }

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => a.Code.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()) || a.QuestionDetails.FirstOrDefault().Body.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<QuestionFilterPaginationModel>();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter._selectedItemBankSignature))
                    {
                        if (filter._selectedBranchId > 0)
                        {
                            query = query.Where(q => q.ItemBankId == filter._selectedBranchId);
                        }
                        else
                        {
                            query = query.Where(q => q.ItemBank.ItemBankSignature == filter._selectedItemBankSignature);
                        }
                    }
                    if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }
                    if (filter._selectedCategory > 0)
                    {
                        query = query.Where(q => q.QuestionCategoryId == filter._selectedCategory);
                    }
                    if (filter._selectedType > 0)
                    {
                        query = query.Where(q => q.QuestionType.Id == filter._selectedType);
                    }
                    if (filter._selectedStatus > 0)
                    {
                        query = query.Where(q => q.QuestionStatus == filter._selectedStatus);
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoQuestionsFoundMatchingCriteria);
            }

            var paginatedQuestions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (!paginatedQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMoreQuestionsFound);
            }

            var questionsDto = _mapper.Map<List<PendingQuestionPaginationDto>>(paginatedQuestions);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.QuestionsRetrievedSuccessfully, new CustomTableData<PendingQuestionPaginationDto>(questionsDto.ToList(), totalRecords));
        }
        public async Task<ApiResponse> GetAllQuestionForPendingOnlyPage(PaginationSearchModel paginationSearchModel)
        {
            if (!Guid.TryParse(_filterParamsValues.UserId, out Guid currentUserId))
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.InternalServerError, HttpStatusCode.Unauthorized, Resource.UserNotFound);
            }

            var committeeItemBankRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeItemBank, long>();

            var userItemBankIdsQuery = committeeItemBankRepo
                .GetAll(ci => ci.Committee.Members.Any(m => m.UserId == currentUserId))
                .Select(ci => ci.ItemBankId)
                .Distinct();

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => x.IsRoot && (x.ParentId == null || x.ParentId == 0) && x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending, null, $"{nameof(QuestionMetadata.QuestionDetails)}.{nameof(Language)},{nameof(QuestionMetadata.QuestionType)},{nameof(QuestionMetadata.QuestionCategory)},{nameof(QuestionMetadata.ItemBank)},{nameof(QuestionMetadata.Subject)}");

            var isSuperOrEntityAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isSuperOrEntityAdmin)
            {
                query = query.Where(q => userItemBankIdsQuery.Contains(q.ItemBankId));
            }

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => (a.Code != null && a.Code.ToLower().Contains(paginationSearchModel.SearchKey.ToLower())) ||
                                         a.QuestionDetails.FirstOrDefault().Body.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                // Deserialize explicitly into the desired class
                var filter = jsonElement.Deserialize<QuestionFilterPaginationModel>();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter._selectedItemBankSignature))
                    {
                        if (filter._selectedBranchId > 0)
                        {
                            query = query.Where(q => q.ItemBankId == filter._selectedBranchId);
                        }
                        else
                        {
                            query = query.Where(q => q.ItemBank.ItemBankSignature == filter._selectedItemBankSignature);
                        }
                    }
                    if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }
                    if (filter._selectedCategory > 0)
                    {
                        query = query.Where(q => q.QuestionCategoryId == filter._selectedCategory);
                    }
                    if (filter._selectedType > 0)
                    {
                        query = query.Where(q => q.QuestionType.Id == filter._selectedType);
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoQuestionsFoundMatchingCriteria);
            }

            var paginatedQuestions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (!paginatedQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMoreQuestionsFound);
            }

            var questionsDto = _mapper.Map<List<PendingQuestionPaginationDto>>(paginatedQuestions);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.QuestionsRetrievedSuccessfully, new CustomTableData<PendingQuestionPaginationDto>([.. questionsDto], totalRecords));
        }

        public async Task<ApiResponse> GetAllApprovedQuestionsForBlocksAsync(PaginationSearchModel paginationSearchModel)
        {
            const string includes = "QuestionDetails.Language,QuestionType,QuestionCategory,ItemBank,Subject,DifficultyProfile,DifficultyLevel";

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => x.IsRoot && (x.ParentId == null || x.ParentId == 0) && x.QuestionStatus == QuestionStatus.Approved && x.CurrentExhaustionCount < x.QuestionsExhaustionCount, null, includes)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => (a.Code != null && a.Code.ToLower().Contains(paginationSearchModel.SearchKey.ToLower())) ||
                                         a.QuestionDetails.FirstOrDefault().Body.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));

                var searchKey = paginationSearchModel.SearchKey.ToLower();

                if (paginationSearchModel.SearchInName)
                {
                    query = query.Where(a => a.Code.ToLower().Contains(searchKey));
                }
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                // Deserialize explicitly into the desired class
                var filter = jsonElement.Deserialize<QuestionFilterPaginationModel>();

                if (filter != null)
                {
                    if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }
                    if (filter._selectedType > 0)
                    {
                        query = query.Where(q => q.QuestionType.Id == filter._selectedType);
                    }
                    if (filter._selectedDeltaType > 0)
                    {
                        query = query.Where(q => q.DifficultyLevel.DeltaType.Id == filter._selectedDeltaType);
                    }
                    if (filter._isConsiderDifficulty && filter._selectedDifficultyLevel > 0)
                    {
                        query = query.Where(q => q.DifficultyLevelId == filter._selectedDifficultyLevel);
                    }
                    if (filter._selectedLanguage > 0)
                    {
                        query = query.Where(q => q.QuestionDetails.Any(qd => qd.LanguageId == filter._selectedLanguage));
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No question found!");
            }

            var paginatedQuestions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (!paginatedQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No more questions found!");
            }

            var questionsDto = _mapper.Map<List<ApprovedQuestionsPaginationDto>>(paginatedQuestions);

            for (int i = 0; i < questionsDto.Count; i++)
            {
                var question = paginatedQuestions[i];
                var languages = question.QuestionDetails
                    .Where(qd => qd.Language != null)
                    .Select(qd => qd.Language.Name);

                questionsDto[i].Language = string.Join(", ", languages);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "Approved questions fetched successfully!", new CustomTableData<ApprovedQuestionsPaginationDto>(questionsDto, totalRecords));
        }

        public async Task<ApiResponse> GetQuestionByMetaDataIdAndLanguageId(long metaDataId, long languageId)
        {
            var questionDetailsEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .Where(x => x.QuestionMetadataId == metaDataId && x.LanguageId == languageId)
                .Include(x => x.QuestionsChoices)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.FileUploadResponseSettings)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.FormQuestions)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.BlocksQuestions)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.ManualPaperItemBankQuestionSections)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.ParentSubQuestion)
                        .ThenInclude(p => p.FormQuestions)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.ParentSubQuestion)
                        .ThenInclude(p => p.BlocksQuestions)
                .Include(x => x.QuestionMetadata)
                    .ThenInclude(qm => qm.ParentSubQuestion)
                        .ThenInclude(p => p.ManualPaperItemBankQuestionSections)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (questionDetailsEntity is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CannotFindQuestionDetailsWithTheGivenId
                );
            }

            var mappedData = _mapper.Map<QuestionDataDto>(questionDetailsEntity);
            mappedData.FileUploadSettings = _mapper.Map<FileUploadSettingsDto>(questionDetailsEntity.QuestionMetadata?.FileUploadResponseSettings);
            mappedData.IsUsedInExam = false;

            if (mappedData.HasShuffled)
                mappedData.Choices.Shuffle();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfully,
                mappedData
            );
        }

        public async Task<ApiResponse> GetQuestionDetailsByIdAsync(long questionDetailsId)
        {
            var questionDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .Where(x => x.Id == questionDetailsId)
                .Include(x => x.QuestionsChoices)
                .Include(x => x.QuestionMetadata)
                .ThenInclude(x => x.FileUploadResponseSettings)
                .Include(x => x.QuestionMetadata.FormQuestions)
                .Include(x => x.QuestionMetadata.BlocksQuestions)
                .Include(x => x.QuestionMetadata.ManualPaperItemBankQuestionSections)
                .Include(x => x.QuestionMetadata.ParentSubQuestion)
                    .ThenInclude(p => p.FormQuestions)
                .Include(x => x.QuestionMetadata.ParentSubQuestion)
                    .ThenInclude(p => p.BlocksQuestions)
                .Include(x => x.QuestionMetadata.ParentSubQuestion)
                    .ThenInclude(p => p.ManualPaperItemBankQuestionSections)
                .SingleOrDefaultAsync();

            if (questionDetails is not null)
            {
                var questionDetailsDto = _mapper.Map<QuestionDetails, QuestionDetailsDto>(questionDetails);

                questionDetailsDto.FileUploadSettings = _mapper.Map<FileUploadSettingsDto>(questionDetails.QuestionMetadata?.FileUploadResponseSettings);

                questionDetailsDto.IsUsedInExam = false;

                if (questionDetailsDto.HasShuffled)
                {
                    questionDetailsDto.Choices.Shuffle();
                }

                return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                string.Empty,
                                questionDetailsDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.CannotFindQuestionDetailsWithTheGivenId);
        }

        public async Task<ApiResponse> GetQuestionMetadataByIdAsync(long questionMetadataId)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == questionMetadataId)
                .Include(qm => qm.ItemBank)
                .Include(qm => qm.Ilo)
                .Include(qm => qm.FileUploadResponseSettings)
                .Include(qm => qm.QuestionGroups)
                .ThenInclude(qg => qg.OESGroup)
                .SingleOrDefaultAsync();

            if (questionMetadata is not null)
            {
                var iloSignature = questionMetadata.Ilo?.ILOSignature;

                var itemBankSignature = questionMetadata.ItemBank?.ItemBankSignature;

                var questionMetadataDto = _mapper.Map<QuestionMetadataRetrievalDto>(questionMetadata);

                questionMetadataDto.OESGroupDtos ??= [];

                questionMetadataDto.OESGroupDtos = [.. questionMetadata
                    .QuestionGroups
                    .Select(qg => new GetOESGroupDto
                    {
                        Id = qg.OESGroup.Id,
                        Name = qg.OESGroup.Name,
                        AutoCreatedForUser = qg.OESGroup.AutoCreatedForUser
                    })];

                if (questionMetadata.FileUploadResponseSettings != null && questionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
                {
                    questionMetadataDto.FileUploadSettings = new FileUploadSettingsDto
                    {
                        Id = questionMetadata.FileUploadResponseSettings.Id,
                        QuestionMetadataId = questionMetadata.FileUploadResponseSettings.QuestionMetadataId,
                        ShowAnswerTextArea = questionMetadata.FileUploadResponseSettings.ShowAnswerTextArea,
                        SupportedFileExtensions = questionMetadata.FileUploadResponseSettings.SupportedFileExtensions,
                        UploadedFilesCount = questionMetadata.FileUploadResponseSettings.UploadedFilesCount,
                        SingleFileMaxSizeInMB = questionMetadata.FileUploadResponseSettings.SingleFileMaxSizeInMB
                    };
                }

                if (!string.IsNullOrWhiteSpace(iloSignature))
                {
                    var parentIlo = await _commonService
                        ._unitOfWork
                        .Repository<ILO, long>()
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ilo => ilo.ILOSignature == iloSignature && ilo.ParentId == null);

                    if (parentIlo != null)
                    {
                        questionMetadataDto.RootIloId = parentIlo.Id;
                    }
                }

                if (!string.IsNullOrWhiteSpace(itemBankSignature))
                {
                    var parentItemBank = await _commonService
                        ._unitOfWork
                        .Repository<ItemBank, long>()
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ib => ib.ItemBankSignature == itemBankSignature && ib.ParentId == null);

                    if (parentItemBank != null)
                    {
                        questionMetadataDto.RootItemBankId = parentItemBank.Id;
                    }
                }

                return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                string.Empty,
                                questionMetadataDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.CannotFindQuestionMetadataWithTheGivenId);
        }

        public async Task<ApiResponse> GetAllQuestionTemplatesAsync(PaginationSearchModel paginationSearch, bool isFromQuestionAI)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QuestionTemplate, long>()
                .GetAll()
                .AsQueryable()
                .Where(q => q.IsFromQuestionAI == isFromQuestionAI);

            if (!string.IsNullOrEmpty(paginationSearch.SearchKey) && paginationSearch.SearchInName)
            {
                var searchKeyLower = paginationSearch.SearchKey.ToLower();

                query = query.Where(a => a.Name.ToLower().Contains(searchKeyLower));
            }

            if (paginationSearch.FromDate.HasValue)
                query = query.Where(a => a.CreationDate >= paginationSearch.FromDate.Value);

            if (paginationSearch.ToDate.HasValue)
                query = query.Where(a => a.CreationDate <= paginationSearch.ToDate.Value);

            if (!query.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No templates found");

            query = paginationSearch.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No templates found");

            var pageIndex = Math.Max(0, paginationSearch.PageIndex);

            var pageSize = Math.Max(1, paginationSearch.PageSize);

            var paginatedTemplates = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            if (!paginatedTemplates.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No more templates found");

            var templateDtos = paginatedTemplates.ConvertAll(q => _mapper.Map<QuestionTemplateDto>(q));

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.TemplateFound,
                                new CustomTableData<QuestionTemplateDto>(templateDtos, totalRecords));
        }

        public async Task<ApiResponse> GetQuestionTemplateByIdAsync(long questionTemplateId)
        {
            var template = await _commonService._unitOfWork.Repository<QuestionTemplate, long>().GetByIdAsync(questionTemplateId);

            if (template == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "Template not found");

            GetQuestionTemplateResponseDto getQuestionTemplateResponseDto = new();

            if (template.IsFromQuestionAI)
            {
                var deserializedData = JsonConvert.DeserializeObject<AIQuestionTemplateCreationDto>(template.Data);

                if (deserializedData == null)
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, "Invalid data format");

                var questionMetaDataDto = new AIQuestionTemplateCreationDto()
                {
                    SelectedItemBankRoot = deserializedData.SelectedItemBankRoot,
                    SelectedItemBankNodeFromDialogDto = deserializedData.SelectedItemBankNodeFromDialogDto,
                    SelectedIloNodeFromDialogDto = deserializedData.SelectedIloNodeFromDialogDto ?? new(),
                    SelectedIloRoot = deserializedData.SelectedIloRoot,
                    SelectedSubject = deserializedData.SelectedSubject,
                    SelectedCategory = deserializedData.SelectedCategory,
                    SelectedLanguage = deserializedData.SelectedLanguage,
                    SelectedDifficultyProfile = deserializedData.SelectedDifficultyProfile,
                    QuestionTypeRequests = deserializedData.QuestionTypeRequests
                };

                getQuestionTemplateResponseDto.AIQuestionTemplate = questionMetaDataDto;
            }
            else
            {
                var deserializedData = JsonConvert.DeserializeObject<QuestionMetadataAdditionOrUpdateDto>(template.Data);

                if (deserializedData == null)
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, "Invalid data format");

                var childItemBank = (await _commonService._unitOfWork.Repository<ItemBank, long>().GetObjAsync(x => x.Id == deserializedData.ItemBankId));

                var rootItemBank = await _commonService._unitOfWork.Repository<ItemBank, long>().GetObjAsync(x => x.ItemBankSignature == childItemBank.ItemBankSignature && x.ParentId == null);

                var childIlo = (await _commonService._unitOfWork.Repository<ILO, long>().GetObjAsync(x => x.Id == deserializedData.IloId));

                var rootIlo = await _commonService._unitOfWork.Repository<ILO, long>().GetObjAsync(x => childIlo != null && x.ILOSignature == childIlo.ILOSignature && x.ParentId == null);

                SelectedIloNodeFromDialogDto childIloDto = childIlo is null ? null : new() { Id = childIlo.Id, Name = childIlo.Name };

                var questionMetaDataDto = new QuestionMetadataRetrievalDto()
                {
                    Author = deserializedData.Author,
                    Code = deserializedData.Code,
                    Delta = deserializedData.Delta,
                    DifficultyLevelId = deserializedData.DifficultyLevelId,
                    DifficultyProfileId = deserializedData.DifficultyProfileId,
                    IsRoot = deserializedData.IsRoot,
                    QuestionsExhaustionCount = deserializedData.QuestionsExhaustionCount,
                    MaximumAnswerTime = deserializedData.MaximumAnswerTime,
                    QuestionCategoryId = deserializedData.QuestionCategoryId,
                    QuestionTypeId = deserializedData.QuestionTypeId,
                    SubjectId = deserializedData.QuestionSubjectId,
                    ChildItemBankDto = new() { Id = childItemBank.Id, Name = childItemBank.Name },
                    RootItemBankId = rootItemBank.Id,
                    ChildIloDto = childIloDto,
                    RootIloId = rootIlo?.Id,
                    ScientificEditorPanelEnabled = deserializedData.ScientificEditorPanelEnabled,
                    FileManagerEditorPanelEnabled = deserializedData.FileManagerEditorPanelEnabled,
                };

                getQuestionTemplateResponseDto.QuestionMetadata = questionMetaDataDto;
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "Template found", getQuestionTemplateResponseDto);
        }

        public async Task<ApiResponse> GetComprehensionSubQuestionsAsync(long rootComprehensionQuestionMetadataId, long languageId)
        {
            var targetComprehensionSubQuestions = await _commonService
               ._unitOfWork
               .Repository<QuestionDetails, long>()
               .Query()
               .Where(qd => qd.QuestionMetadata.ParentId == rootComprehensionQuestionMetadataId && qd.LanguageId == languageId)
               .Include(qd => qd.QuestionsChoices)
               .Include(qd => qd.QuestionMetadata)
               .ThenInclude(qm => qm.QuestionType)
               .ToListAsync();

            if (targetComprehensionSubQuestions.Count > 0)
            {
                var mappedSubQuestions = _mapper.Map<List<SubQuestionDetailsDto>>(targetComprehensionSubQuestions);

                var parentMetadata = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .GetObjAsync(x => x.Id == rootComprehensionQuestionMetadataId, Including: "FormQuestions,BlocksQuestions,ManualPaperItemBankQuestionSections");

                bool isParentUsed = false;

                foreach (var questionDetail in mappedSubQuestions)
                {
                    questionDetail.IsUsedInExam = isParentUsed;

                    if (questionDetail.HasShuffled)
                    {
                        questionDetail.Choices.Shuffle();
                    }
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.ComprehensionSubQuestionsFetchedSuccessfully,
                                    mappedSubQuestions);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.ComprehensionSubQuestionsArgumentsNotFound);
        }

        public async Task<ApiResponse> AddQuestionMetadataAsync(QuestionMetadataAdditionOrUpdateDto questionMetaData)
        {
            if (questionMetaData == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.QuestionMetadataCannotBeNull
                );
            }

            var validatorResult = _questionValidatorService.ValidateAddedOrUpdatedQuestionMetadata(questionMetaData);

            bool canUseItemBank = await _itemBankAuthorizationService.CanUseItemBankAsync(questionMetaData.ItemBankId);

            if (!canUseItemBank)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.YouAreNotAuthorizedToUseThisItemBank
                );
            }

            if (validatorResult.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return validatorResult;
            }

            var trimmedQuestionCode = questionMetaData.Code.Trim();

            var organizationId = _filterParamsValues.OrganizationId;

            bool codeExists = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(q => q.Code.Trim() == trimmedQuestionCode && q.OrganizationId == organizationId && !q.IsDeleted);

            if (codeExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.TheCodeAlreadyExistsInThisOrganizationPleaseWriteAnotherCode
                );
            }

            var itemBankUnscored = await GetItemBankUnscoredAsync(questionMetaData.ItemBankId);

            QuestionMetadata question = new()
            {
                Code = questionMetaData.Code,
                SubjectId = questionMetaData.QuestionSubjectId,
                QuestionTypeId = questionMetaData.QuestionTypeId,
                QuestionCategoryId = questionMetaData.QuestionCategoryId,
                DifficultyProfileId = questionMetaData.DifficultyProfileId,
                DifficultyLevelId = questionMetaData.DifficultyLevelId,
                IloId = questionMetaData.IloId,
                IsActive = true,
                IsRoot = questionMetaData.IsRoot,
                MaximumAnswerTime = questionMetaData.MaximumAnswerTime,
                ItemBankId = questionMetaData.ItemBankId,
                Author = questionMetaData.Author,
                Delta = questionMetaData.Delta,
                QuestionStatus = QuestionStatus.LayoutSelectedAndPending,
                QuestionsExhaustionCount = questionMetaData.QuestionsExhaustionCount,
                ScientificEditorPanelEnabled = questionMetaData.ScientificEditorPanelEnabled,
                FileManagerEditorPanelEnabled = questionMetaData.FileManagerEditorPanelEnabled,
                Unscored = itemBankUnscored
            };

            if (questionMetaData.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                var fileUploadSettings = new FileUploadResponseSettings
                {
                    ShowAnswerTextArea = questionMetaData.FileUploadResponseSettings.ShowAnswerTextArea,
                    SupportedFileExtensions = questionMetaData.FileUploadResponseSettings.SupportedFileExtensions,
                    UploadedFilesCount = questionMetaData.FileUploadResponseSettings.UploadedFilesCount,
                    SingleFileMaxSizeInMB = questionMetaData.FileUploadResponseSettings.SingleFileMaxSizeInMB
                };

                question.FileUploadResponseSettings = fileUploadSettings;
            }

            await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .AddAsync(question);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                _ = Guid.TryParse(_filterParamsValues.UserId, out Guid parsedUserId);

                var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

                if (!isSuperAdmin)
                {
                    await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(
                        new AutoPermissionAssignmentRequest
                        {
                            EntityId = question.Id,
                            EntityName = question.Code,
                            ResourceType = ResourceType.Questions,
                            UserId = parsedUserId,
                            AdditionalGroupIds = questionMetaData.OESGroupDtos?.Select(x => x.Id).ToList(),
                            EntityGroupType = typeof(QuestionGroups)
                        });
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.QuestionAddedSuccessfullyWithGroups,
                                    question.Id);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.SomethingWentWrong);
        }

        public async Task<ApiResponse> AddQuestionTemplateAsync(QuestionCreationTemplateDto questionTemplateDto)
        {
            var validatorResult = _questionValidatorService.ValidateAddedOrUpdatedQuestionMetadata(questionTemplateDto);

            if (validatorResult.CustomCodeStatus != CustomCodeStatus.Success && !questionTemplateDto.IsFromQuestionAI)
            {
                return validatorResult;
            }
            else
            {
                var isNameExists = await _commonService
                   ._unitOfWork
                   .Repository<QuestionTemplate, long>()
                   .IsExistAsync(q => q.Name.ToLower() == questionTemplateDto.Name.ToLower());

                if (isNameExists)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.Conflict,
                                        Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
                }

                var _object = JsonSerializer.Serialize(questionTemplateDto);
                var jObject = JObject.Parse(_object);
                jObject.Remove(nameof(QuestionCreationTemplateDto.Name));
                jObject.Remove(nameof(QuestionCreationTemplateDto.Id));
                jObject.Remove(nameof(QuestionCreationTemplateDto.QuestionLayoutId));

                QuestionTemplate template = new()
                {
                    Name = questionTemplateDto.Name,
                    Data = jObject.ToString()
                };

                await _commonService
                ._unitOfWork
                .Repository<QuestionTemplate, long>()
                .AddAsync(template);

                if (await _commonService._unitOfWork.Complete() > 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Templatehasbeenaddedsuccessfully,
                                        template.Id);
                }
                else
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.SomethingWentWrong);
                }
            }
        }

        public async Task<ApiResponse> AddTemplateAIQuestionAsync(AIQuestionTemplateCreationDto dto)
        {
            var isNameExists = await _commonService
               ._unitOfWork
               .Repository<QuestionTemplate, long>()
               .IsExistAsync(q => q.Name.ToLower() == dto.Name.ToLower());

            if (isNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var _object = JsonSerializer.Serialize(dto);
            var jObject = JObject.Parse(_object);
            jObject.Remove(nameof(QuestionCreationTemplateDto.Name));

            QuestionTemplate template = new()
            {
                Name = dto.Name,
                Data = jObject.ToString(),
                IsFromQuestionAI = dto.IsFromAI
            };

            await _commonService
                ._unitOfWork
                .Repository<QuestionTemplate, long>()
                .AddAsync(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Templatehasbeenaddedsuccessfully,
                                    template.Id);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.SomethingWentWrong);
            }
        }

        public async Task<ApiResponse> AddNewQuestionLanguageDetails(QuestionDetailsDto questionDetailsDto)
        {
            var ValidatorResult = await _questionValidatorService.ValidateNewLanguageDetails(questionDetailsDto);

            if (ValidatorResult.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return ValidatorResult;
            }
            else
            {
                var questionMetadataInfo = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .Query()
                    .Where(x => x.Id == questionDetailsDto.QuestionMetadataId)
                    .Select(x => new { QuestionTypeName = x.QuestionType.Name, x.QuestionTypeId, x.QuestionStatus })
                    .FirstOrDefaultAsync();

                var questionType = GetQuestionType(questionMetadataInfo.QuestionTypeName);

                if (questionDetailsDto.UseArabicNumbers)
                {
                    questionDetailsDto.Body = ConvertDigitsToArabicInText(questionDetailsDto.Body);
                    questionDetailsDto.Instructions = ConvertDigitsToArabicInText(questionDetailsDto.Instructions);
                    questionDetailsDto.ModelAnswer = ConvertModelAnswerDigitsForQuestionType(questionDetailsDto.ModelAnswer, (QuestionTypeEnum)questionType);

                    if (questionDetailsDto.Choices?.Count > 0)
                    {
                        foreach (var choice in questionDetailsDto.Choices)
                        {
                            choice.ChoiceText = ConvertDigitsToArabicInText(choice.ChoiceText);
                        }
                    }
                }

                QuestionDetails questionDetails = new()
                {
                    Body = questionDetailsDto.Body,
                    LanguageId = questionDetailsDto.LanguageId,
                    UseArabicNumbers = questionDetailsDto.UseArabicNumbers,
                    QuestionMetadataId = questionDetailsDto.QuestionMetadataId,
                    Instructions = questionDetailsDto.Instructions,
                    ModelAnswer = questionDetailsDto.ModelAnswer,
                    HasShuffled = questionDetailsDto.HasShuffled,
                    MaxRecordingTimeInSeconds = questionDetailsDto.MaxRecordingTimeInSeconds,
                    MaxWords = questionDetailsDto.MaxWords,
                    QuestionsChoices = questionDetailsDto
                    .Choices
                    .ConvertAll(choice => new QuestionsChoices
                    {
                        ChoiceText = choice.ChoiceText,
                        IsCorrectAnswer = choice.IsCorrectAnswer,
                        AttachmentFileName = choice.AttachmentFileName,
                        OrderId = choice.OrderId
                    })
                };

                if (questionMetadataInfo?.QuestionStatus == QuestionStatus.Approved)
                {
                    var questionMetadataEntity = await _commonService
                        ._unitOfWork
                        .Repository<QuestionMetadata, long>()
                        .GetByIdAsync(questionDetailsDto.QuestionMetadataId);

                    if (questionMetadataEntity != null)
                        questionMetadataEntity.IsReplaced = true;
                }

                await _commonService
                    ._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .AddAsync(questionDetails);

                if (await _commonService._unitOfWork.Complete() > 0)
                {
                    await HandleOrderingQuestionModelAnswer(questionDetails, questionMetadataInfo.QuestionTypeId);

                    var extractedFiles = _htmlHelperService.ExtractMediaEntitiesFromQuestionDetails([questionDetails]);

                    if (extractedFiles.Count > 0)
                    {
                        _commonService
                            ._unitOfWork
                            .Repository<QuestionDocLibFile, long>()
                            .AddRangAsync(extractedFiles);

                        await _commonService._unitOfWork.Complete();
                    }

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.QuestionDetailsAddedSuccessfully
                    );
                }
                else
                {
                    _commonService._unitOfWork.Dispose();

                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.FailToAddQuestionDetails);
                }
            }
        }

        public async Task<ApiResponse> DeleteQuestionTemplateAsync(long templateId)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<QuestionTemplate, long>()
                .GetObjAsync(x => x.Id == templateId);

            if (template is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.NotFound,
                                    Resource.TemplateNotFound);
            }

            _commonService._unitOfWork.Repository<QuestionTemplate, long>().Delete(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.TemplateDeletedSuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.FailedToDeleteTemplate);
        }

        public async Task<ApiResponse> EditQuestionDetails(QuestionDetailsDto questionDetailsDto)
        {
            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == questionDetailsDto.QuestionMetadataId)
                .Include(qm => qm.QuestionType)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.QuestionsChoices)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.SegmentQuestionProperties)
                .Include(qm => qm.FormQuestions)
                .Include(qm => qm.BlocksQuestions)
                .Include(qm => qm.ManualPaperItemBankQuestionSections)
                .FirstOrDefaultAsync();

            var validationResult = ValidateUpdatedQuestionDetails(questionMetadataEntity, questionDetailsDto);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message,
                                    null!);
            }

            var questionType = GetQuestionType(questionMetadataEntity.QuestionType.Name);

            var questionDetailsEntity = questionMetadataEntity.QuestionDetails.FirstOrDefault(qd => qd.Id == questionDetailsDto.Id);

            var isUsed = false;

            if (questionDetailsEntity != null && questionMetadataEntity.QuestionStatus == QuestionStatus.Approved)
            {
                questionMetadataEntity.IsReplaced = true;

                if (HasQuestionDetailsChanged(questionDetailsEntity, questionDetailsDto, (QuestionTypeEnum)questionType))
                {
                    if (questionMetadataEntity.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension)
                    {
                        await CreateComprehensionQuestionVersions(questionMetadataEntity.Id, questionDetailsEntity.LanguageId);
                    }
                    else
                    {
                        await CreateQuestionVersion(questionDetailsEntity);
                    }
                }
            }

            var existingChoices = questionDetailsEntity!.QuestionsChoices.ToList();
            var incomingChoices = questionDetailsDto.Choices ?? [];
            foreach (var existingChoice in existingChoices)
            {
                var matchingIncomingChoice = incomingChoices.FirstOrDefault(c => c.Id == existingChoice.Id);

                if (matchingIncomingChoice != null)
                {
                    if (isUsed)
                    {
                        if (existingChoice.ChoiceText != matchingIncomingChoice.ChoiceText ||
                            existingChoice.IsCorrectAnswer != matchingIncomingChoice.IsCorrectAnswer ||
                            existingChoice.OrderId != matchingIncomingChoice.OrderId)
                        {
                            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, Resource.CannotUpdateExistingChoicesInUsedQuestion);
                        }
                    }

                    existingChoice.ChoiceText = matchingIncomingChoice.ChoiceText;
                    existingChoice.IsCorrectAnswer = matchingIncomingChoice.IsCorrectAnswer;
                    existingChoice.OrderId = matchingIncomingChoice.OrderId;
                    existingChoice.AttachmentFileName = matchingIncomingChoice.AttachmentFileName;
                }
                else
                {
                    if (isUsed)
                    {
                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, Resource.CannotDeleteExistingChoicesInUsedQuestion);
                    }

                    _commonService._unitOfWork.Repository<QuestionsChoices, long>().Delete(existingChoice);
                }
            }

            var newChoices = incomingChoices.Where(c => c.Id == 0).ToList();
            foreach (var newChoiceDto in newChoices)
            {
                if (isUsed)
                {
                    newChoiceDto.IsCorrectAnswer = false;
                }

                var newEntity = _mapper.Map<QuestionsChoices>(newChoiceDto);
                questionDetailsEntity.QuestionsChoices.Add(newEntity);
            }

            if (questionDetailsDto.UseArabicNumbers)
            {
                questionDetailsDto.Body = ConvertDigitsToArabicInText(questionDetailsDto.Body);
                questionDetailsDto.Instructions = ConvertDigitsToArabicInText(questionDetailsDto.Instructions);
                questionDetailsDto.ModelAnswer = ConvertModelAnswerDigitsForQuestionType(questionDetailsDto.ModelAnswer, (QuestionTypeEnum)questionType);

                if (questionDetailsDto.Choices?.Count > 0)
                {
                    foreach (var choice in questionDetailsDto.Choices)
                    {
                        choice.ChoiceText = ConvertDigitsToArabicInText(choice.ChoiceText);
                    }
                }
            }

            var mappedQuestionDetails = _mapper.Map(questionDetailsDto, questionDetailsEntity);

            var extractedFiles = new List<FileUrlWithFileId>();

            extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(questionDetailsDto.Body));

            foreach (var choice in questionDetailsDto.Choices)
            {
                extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(choice.ChoiceText));
            }

            void AddRawAttachmentIfPresent(string attachmentFileName)
            {
                if (!string.IsNullOrWhiteSpace(attachmentFileName) && Guid.TryParse(attachmentFileName, out _))
                {
                    extractedFiles.Add(new FileUrlWithFileId
                    {
                        FileId = attachmentFileName,
                        Url = $"{CentralizedUrlHelper.DocLibApiBaseUrl.TrimEnd('/')}/api/Document/DownloadStream?documentId={attachmentFileName}"
                    });
                }
            }

            AddRawAttachmentIfPresent(questionDetailsDto.AttachmentFileName);

            foreach (var choice in questionDetailsDto.Choices)
            {
                AddRawAttachmentIfPresent(choice.AttachmentFileName);
            }

            var existingMediaFiles = await _commonService
                ._unitOfWork
                .Repository<QuestionDocLibFile, long>()
                .GetAll(qf => qf.QuestionId == questionDetailsDto.QuestionMetadataId)
                .ToListAsync();

            if (existingMediaFiles.Any())
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .DeleteRange(existingMediaFiles);
            }

            if (extractedFiles.Count > 0)
            {
                var mediaEntities = extractedFiles
                    .DistinctBy(x => x.FileId)
                    .Select(x => new QuestionDocLibFile
                    {
                        QuestionId = questionDetailsDto.QuestionMetadataId,
                        FileURL = x.Url,
                        FileId = Guid.Parse(x.FileId)
                    }).ToList();

                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangAsync(mediaEntities);
            }

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                await HandleOrderingQuestionModelAnswer(mappedQuestionDetails, questionMetadataEntity.QuestionTypeId);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Questiondetailsupdatedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.FailToupdatequestiondetails);
        }

        public async Task<ApiResponse> DeleteQuestionDetails(long questionDetailsId)
        {
            var questionDetailsEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetObjAsync(
                    Filter: q => q.Id == questionDetailsId,
                    Including: nameof(QuestionDetails.QuestionsChoices)
                );

            if (questionDetailsEntity is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SomethingWentWrongwhileDeletingQuestionDtails);
            }

            var relatedDocFiles = await _commonService
                ._unitOfWork
                .Repository<QuestionDocLibFile, long>()
                .GetAll(q => q.QuestionId == questionDetailsEntity.QuestionMetadataId)
                .ToListAsync();

            var questionMetadataEntity = await _commonService
                    ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetByIdAsync(questionDetailsEntity.QuestionMetadataId);

            if (questionMetadataEntity != null && questionMetadataEntity.QuestionStatus == QuestionStatus.Approved)
            {
                questionMetadataEntity.IsReplaced = true;

                if (questionMetadataEntity.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension)
                {
                    await CreateComprehensionQuestionVersions(questionMetadataEntity.Id, questionDetailsEntity.LanguageId);
                }
                else
                {
                    await CreateQuestionVersion(questionDetailsEntity);
                }
            }

            if (relatedDocFiles.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .DeleteRange(relatedDocFiles);
            }

            _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .SoftDelete(questionDetailsEntity);

            await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(x => x.Id == questionDetailsEntity.QuestionMetadataId && x.QuestionStatus == QuestionStatus.Approved)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsReplaced, true));

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Questionhasbeendeletedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.FailedToDeleteQuestion);
        }

        public async Task<ApiResponse> DeleteQuestionMetaData(long questionMetadataId)
        {
            var includes = $"{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}," +
                           $"{nameof(QuestionMetadata.SubQuestions)}.{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}";

            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == questionMetadataId, includes);

            if (questionMetadataEntity is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.NotFound,
                                    string.Format(Resource.QuestionMetadataNotFound, questionMetadataId));
            }

            // Validate that the question is not linked to a paper

            var isQuestionLinkedToPaper = await IsQuestionLinkedToPaperAsync(questionMetadataId);

            if (isQuestionLinkedToPaper)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.Conflict,
                                    string.Format(Resource.QuestionCannotBeDeletedBecauseLinked));
            }

            // TODO by Weam: Check if the question type is comprehension, if so, delete the sub-questions as well

            await DeleteAllQuestionVersionsForMetadataTreeAsync(questionMetadataId);

            var relatedDocFiles = await _commonService
                ._unitOfWork
                .Repository<QuestionDocLibFile, long>()
                .GetAll(q => q.QuestionId == questionMetadataId)
                .ToListAsync();

            if (relatedDocFiles.Any())
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .DeleteRange(relatedDocFiles);
            }

            if (questionMetadataEntity.SubQuestions?.Count > 0)
            {
                foreach (var subQuestion in questionMetadataEntity.SubQuestions)
                {
                    var subQuestionDocFiles = await _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .GetAll(q => q.QuestionId == subQuestion.Id)
                        .ToListAsync();

                    if (subQuestionDocFiles.Count > 0)
                    {
                        _commonService
                            ._unitOfWork
                            .Repository<QuestionDocLibFile, long>()
                            .DeleteRange(subQuestionDocFiles);
                    }

                    _commonService._unitOfWork.Repository<QuestionMetadata, long>().SoftDeleteRecursive(subQuestion);
                }
            }

            _commonService._unitOfWork.Repository<QuestionMetadata, long>().SoftDeleteRecursive(questionMetadataEntity);

            var affectedRows = await _commonService._unitOfWork.Complete();

            return affectedRows > 0
                ? _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Questionandallrelateddatadeletedsuccessfully)
                : _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToPersistDeletion);
        }

        public async Task<ApiResponse> AddComprehensionSubQuestionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            // Here I've used GetAll method although it is a single retrieved object, to utilize the method of AsNoTracking.
            var parentQuestionMetadataEntity = _commonService
               ._unitOfWork
               .Repository<QuestionMetadata, long>()
               .GetAll(m => m.Id == comprehensionSubQuestionDto.SubQuestionMetadataDto.ParentId, Including: nameof(QuestionDetails))
               .AsNoTracking()
               .FirstOrDefault();

            var comprehensionSubQuestionDtoValidationTuple = ValidateComprehensionAddedSubQuestion(parentQuestionMetadataEntity, comprehensionSubQuestionDto);

            if (!comprehensionSubQuestionDtoValidationTuple.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    comprehensionSubQuestionDtoValidationTuple.Message,
                                    null!);
            }

            var trimmedSubQuestionCode = comprehensionSubQuestionDto.SubQuestionMetadataDto.Code?.Trim();
            var organizationId = _filterParamsValues.OrganizationId;

            bool codeExists = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(q => q.Code.Trim() == trimmedSubQuestionCode && q.OrganizationId == organizationId);

            if (codeExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.TheCodeAlreadyExistsInThisOrganizationPleaseWriteAnotherCode
                );
            }

            var subQuestionMetadataEntity = GetPreparedCopyOfRootComprehensionQuestionMetadata(parentQuestionMetadataEntity, comprehensionSubQuestionDto);

            var subQuestionDetailsEntity = _mapper.Map<QuestionDetails>(comprehensionSubQuestionDto.SubQuestionDetailsDto);

            subQuestionDetailsEntity.QuestionsChoices = comprehensionSubQuestionDto
                .SubQuestionDetailsDto
                .Choices?
                .ConvertAll(choice => new QuestionsChoices
                {
                    ChoiceText = choice.ChoiceText,
                    IsCorrectAnswer = choice.IsCorrectAnswer,
                    AttachmentFileName = choice.AttachmentFileName,
                    OrderId = choice.OrderId
                }) ?? [];

            subQuestionDetailsEntity.QuestionMetadata = subQuestionMetadataEntity;

            await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .AddAsync(subQuestionDetailsEntity);

            if (comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings != null && comprehensionSubQuestionDto.SubQuestionMetadataDto.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                var fileUploadSettingsDto = comprehensionSubQuestionDto.SubQuestionMetadataDto.FileUploadSettings;

                var fileUploadSettingsEntity = new FileUploadResponseSettings
                {
                    QuestionMetadata = subQuestionMetadataEntity,
                    QuestionMetadataId = comprehensionSubQuestionDto.SubQuestionDetailsDto.Id,
                    ShowAnswerTextArea = fileUploadSettingsDto.ShowAnswerTextArea,
                    UploadedFilesCount = fileUploadSettingsDto.UploadedFilesCount,
                    SupportedFileExtensions = fileUploadSettingsDto.SupportedFileExtensions,
                    SingleFileMaxSizeInMB = fileUploadSettingsDto.SingleFileMaxSizeInMB
                };

                await _commonService
                    ._unitOfWork
                    .Repository<FileUploadResponseSettings, long>()
                    .AddAsync(fileUploadSettingsEntity);
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var extractedFiles = new List<FileUrlWithFileId>();

                if (subQuestionDetailsEntity.Body != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.Body));

                if (subQuestionDetailsEntity.ModelAnswer != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.ModelAnswer));

                if (subQuestionDetailsEntity.Instructions != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.Instructions));

                if (subQuestionDetailsEntity.QuestionsChoices.Count > 0)
                {
                    foreach (var choice in subQuestionDetailsEntity.QuestionsChoices)
                    {
                        extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(choice.ChoiceText));
                    }
                }

                if (extractedFiles.Count > 0)
                {
                    var mediaEntities = extractedFiles
                        .DistinctBy(x => x.FileId)
                        .Select(x => new QuestionDocLibFile
                        {
                            QuestionId = subQuestionDetailsEntity.QuestionMetadataId,
                            FileURL = x.Url,
                            FileId = Guid.Parse(x.FileId)
                        })
                        .ToList();

                    _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .AddRangAsync(mediaEntities);

                    await _commonService._unitOfWork.Complete();
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Comprehensionsubquestionhasbeenaddedsuccessfully,
                                    null!);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.Somethingwentwrongaddingcomprehensionsubquestion,
                                    null!);
        }

        public async Task<ApiResponse> AddComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            var targetSubQuestionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(m => m.Id == subQuestionLanguageVariantDto.SubQuestionDetailsDto.QuestionMetadataId,
                             Including: $"{nameof(QuestionDetails)},{nameof(FileUploadResponseSettings)}");

            var (IsValid, Message) = ValidateAddedSubQuestionLanguageVariant(targetSubQuestionMetadataEntity, subQuestionLanguageVariantDto);

            if (!IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Message,
                                    null!);
            }

            targetSubQuestionMetadataEntity.QuestionTypeId = subQuestionLanguageVariantDto.SubQuestionMetadataDto.QuestionTypeId;
            targetSubQuestionMetadataEntity.Code = subQuestionLanguageVariantDto.SubQuestionMetadataDto.Code;
            targetSubQuestionMetadataEntity.Delta = subQuestionLanguageVariantDto.SubQuestionMetadataDto.Delta;

            if (targetSubQuestionMetadataEntity.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                var dtoSettings = subQuestionLanguageVariantDto.SubQuestionMetadataDto.FileUploadSettings;

                if (dtoSettings == null)
                {
                    return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Failure,
                                   HttpStatusCode.BadRequest,
                                   Resource.FileUploadSettingsarerequiredforthisquestiontype,
                                   null!);
                }

                if (targetSubQuestionMetadataEntity.FileUploadResponseSettings == null)
                {
                    targetSubQuestionMetadataEntity.FileUploadResponseSettings = new FileUploadResponseSettings
                    {
                        QuestionMetadataId = targetSubQuestionMetadataEntity.Id
                    };

                    await _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .AddAsync(targetSubQuestionMetadataEntity.FileUploadResponseSettings);
                }

                targetSubQuestionMetadataEntity.FileUploadResponseSettings.ShowAnswerTextArea = dtoSettings.ShowAnswerTextArea;
                targetSubQuestionMetadataEntity.FileUploadResponseSettings.SupportedFileExtensions = dtoSettings.SupportedFileExtensions;
                targetSubQuestionMetadataEntity.FileUploadResponseSettings.UploadedFilesCount = dtoSettings.UploadedFilesCount;
                targetSubQuestionMetadataEntity.FileUploadResponseSettings.SingleFileMaxSizeInMB = dtoSettings.SingleFileMaxSizeInMB;
            }
            else
            {
                if (targetSubQuestionMetadataEntity.FileUploadResponseSettings != null)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .Delete(targetSubQuestionMetadataEntity.FileUploadResponseSettings);
                }
            }

            var subQuestionDetailsEntity = _mapper.Map<QuestionDetails>(subQuestionLanguageVariantDto.SubQuestionDetailsDto);

            subQuestionDetailsEntity.QuestionsChoices = subQuestionLanguageVariantDto
                .SubQuestionDetailsDto
                .Choices?
                .ConvertAll(choice => new QuestionsChoices
                {
                    ChoiceText = choice.ChoiceText,
                    IsCorrectAnswer = choice.IsCorrectAnswer,
                    AttachmentFileName = choice.AttachmentFileName,
                    OrderId = choice.OrderId
                }) ?? [];

            subQuestionDetailsEntity.QuestionMetadataId = subQuestionLanguageVariantDto.SubQuestionDetailsDto.QuestionMetadataId;

            await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .AddAsync(subQuestionDetailsEntity);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                await HandleOrderingQuestionModelAnswer(subQuestionDetailsEntity, targetSubQuestionMetadataEntity.QuestionTypeId);

                var extractedFiles = new List<FileUrlWithFileId>();

                if (subQuestionDetailsEntity.Body != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.Body));

                if (subQuestionDetailsEntity.ModelAnswer != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.ModelAnswer));

                if (subQuestionDetailsEntity.Instructions != null)
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(subQuestionDetailsEntity.Instructions));

                if (subQuestionDetailsEntity.QuestionsChoices.Any())
                {
                    foreach (var choice in subQuestionDetailsEntity.QuestionsChoices)
                    {
                        extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(choice.ChoiceText));
                    }
                }

                if (extractedFiles.Any())
                {
                    var mediaEntities = extractedFiles
                        .DistinctBy(x => x.FileId)
                        .Select(x => new QuestionDocLibFile
                        {
                            QuestionId = subQuestionDetailsEntity.QuestionMetadataId,
                            FileURL = x.Url,
                            FileId = Guid.Parse(x.FileId)
                        })
                        .ToList();

                    _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .AddRangAsync(mediaEntities);

                    await _commonService._unitOfWork.Complete();
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Comprehensionsubquestionlanguagehasbeenaddedsuccessfully,
                                    null!);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.Somethingwentwrongaddingcomprehensionsubquestionlanguage,
                                    null!);
        }

        public async Task<ApiResponse> UpdateComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == subQuestionLanguageVariantDto.SubQuestionDetailsDto.QuestionMetadataId)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.QuestionsChoices)
                .Include(qm => qm.FileUploadResponseSettings)
                .Include(qm => qm.ParentSubQuestion)
                    .ThenInclude(p => p.FormQuestions)
                .Include(qm => qm.ParentSubQuestion)
                    .ThenInclude(p => p.BlocksQuestions)
                .Include(qm => qm.ParentSubQuestion)
                    .ThenInclude(p => p.ManualPaperItemBankQuestionSections)
                .FirstOrDefaultAsync();

            if (questionMetadataEntity == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    $"{Resource.SubquestionwithMetadataId} {subQuestionLanguageVariantDto.SubQuestionDetailsDto.QuestionMetadataId} {Resource.NotFound}");
            }

            var isUsed = false;

            var validationResult = await ValidateUpdatedSubQuestionLanguageVariantAsync(questionMetadataEntity, subQuestionLanguageVariantDto);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message,
                                    null!);
            }

            questionMetadataEntity.QuestionTypeId = subQuestionLanguageVariantDto.SubQuestionMetadataDto.QuestionTypeId;

            questionMetadataEntity.Code = subQuestionLanguageVariantDto.SubQuestionMetadataDto.Code;

            questionMetadataEntity.Delta = subQuestionLanguageVariantDto.SubQuestionMetadataDto.Delta;

            if (questionMetadataEntity.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                var dtoSettings = subQuestionLanguageVariantDto.SubQuestionMetadataDto.FileUploadSettings;

                if (dtoSettings == null)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        Resource.FileUploadSettingsarerequiredforthisquestiontype,
                                        null!);
                }

                if (questionMetadataEntity.FileUploadResponseSettings == null)
                {
                    questionMetadataEntity.FileUploadResponseSettings = new FileUploadResponseSettings
                    {
                        QuestionMetadataId = questionMetadataEntity.Id
                    };

                    await _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .AddAsync(questionMetadataEntity.FileUploadResponseSettings);
                }

                questionMetadataEntity.FileUploadResponseSettings.ShowAnswerTextArea = dtoSettings.ShowAnswerTextArea;
                questionMetadataEntity.FileUploadResponseSettings.SupportedFileExtensions = dtoSettings.SupportedFileExtensions;
                questionMetadataEntity.FileUploadResponseSettings.UploadedFilesCount = dtoSettings.UploadedFilesCount;
                questionMetadataEntity.FileUploadResponseSettings.SingleFileMaxSizeInMB = dtoSettings.SingleFileMaxSizeInMB;
            }
            else
            {
                if (questionMetadataEntity.FileUploadResponseSettings != null)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .Delete(questionMetadataEntity.FileUploadResponseSettings);
                }
            }

            var questionDetailsEntity = questionMetadataEntity.QuestionDetails.FirstOrDefault(qd => qd.Id == subQuestionLanguageVariantDto.SubQuestionDetailsDto.Id);

            if (questionMetadataEntity.QuestionStatus == QuestionStatus.Approved)
            {
                bool hasChanges = HasSubQuestionDetailsChanged(questionDetailsEntity!, subQuestionLanguageVariantDto.SubQuestionDetailsDto);

                if (hasChanges && questionMetadataEntity.ParentId.HasValue)
                {
                    await CreateComprehensionQuestionVersions(questionMetadataEntity.ParentId.Value, questionDetailsEntity!.LanguageId);
                }
            }

            var existingChoices = questionDetailsEntity!.QuestionsChoices.ToList();
            var incomingChoices = subQuestionLanguageVariantDto.SubQuestionDetailsDto.Choices ?? [];
            foreach (var existingChoice in existingChoices)
            {
                var matchingIncomingChoice = incomingChoices.FirstOrDefault(c => c.Id == existingChoice.Id);

                if (matchingIncomingChoice != null)
                {
                    if (isUsed)
                    {
                        if (existingChoice.ChoiceText != matchingIncomingChoice.ChoiceText ||
                            existingChoice.IsCorrectAnswer != matchingIncomingChoice.IsCorrectAnswer ||
                            existingChoice.OrderId != matchingIncomingChoice.OrderId)
                        {
                            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, Resource.CannotUpdateExistingChoicesInUsedQuestion);
                        }
                    }

                    existingChoice.ChoiceText = matchingIncomingChoice.ChoiceText;
                    existingChoice.IsCorrectAnswer = matchingIncomingChoice.IsCorrectAnswer;
                    existingChoice.OrderId = matchingIncomingChoice.OrderId;
                    existingChoice.AttachmentFileName = matchingIncomingChoice.AttachmentFileName;
                }
                else
                {
                    if (isUsed)
                    {
                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, Resource.CannotDeleteExistingChoicesInUsedQuestion);
                    }

                    _commonService._unitOfWork.Repository<QuestionsChoices, long>().Delete(existingChoice);
                }
            }

            var newChoices = incomingChoices.Where(c => c.Id == 0).ToList();
            foreach (var newChoiceDto in newChoices)
            {
                if (isUsed)
                {
                    newChoiceDto.IsCorrectAnswer = false;
                }

                var newEntity = _mapper.Map<QuestionsChoices>(newChoiceDto);
                questionDetailsEntity.QuestionsChoices.Add(newEntity);
            }

            var mappedQuestionDetails = _mapper.Map(subQuestionLanguageVariantDto.SubQuestionDetailsDto, questionDetailsEntity);

            var extractedFiles = new List<FileUrlWithFileId>();

            extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(mappedQuestionDetails.Body));

            foreach (var choice in mappedQuestionDetails.QuestionsChoices)
            {
                extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(choice.ChoiceText));
            }

            var existingMediaFiles = await _commonService
                ._unitOfWork
                .Repository<QuestionDocLibFile, long>()
                .GetAll(qf => qf.QuestionId == mappedQuestionDetails.QuestionMetadataId)
                .ToListAsync();

            if (existingMediaFiles.Any())
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .DeleteRange(existingMediaFiles);
            }

            if (extractedFiles.Any())
            {
                var mediaEntities = extractedFiles
                    .DistinctBy(x => x.FileId)
                    .Select(x => new QuestionDocLibFile
                    {
                        QuestionId = mappedQuestionDetails.QuestionMetadataId,
                        FileURL = x.Url,
                        FileId = Guid.Parse(x.FileId)
                    }).ToList();

                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangAsync(mediaEntities);
            }

            var result = await _commonService._unitOfWork.Complete();

            if (result >= 0)
            {
                await HandleOrderingQuestionModelAnswer(mappedQuestionDetails, questionMetadataEntity.QuestionTypeId);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Subquestiondetailsupdatedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.Somethingwentwrongupdatingsubquestiondetails);
        }

        public async Task<ApiResponse> DeleteComprehensionSubQuestionLanguageVariantAsync(long subQuestionMetadataId, long subQuestionDetailsId)
        {
            var questionMetadataRepository = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>();

            var questionDetailsRepository = _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>();

            var questionDetailsEntities = (await questionDetailsRepository
                .GetAllAsync(qd => qd.QuestionMetadataId == subQuestionMetadataId))
                .ToList();

            if (questionDetailsEntities.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SubQuestionDetailsDeletionFailed);
            }

            var targetSubQuestionDetailsEntity = questionDetailsEntities.Find(qd => qd.Id == subQuestionDetailsId);

            if (targetSubQuestionDetailsEntity is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SubQuestionDetailsDeletionFailed);
            }

            if (questionDetailsEntities.Count == 1)
            {
                var subQuestionMetadataEntity = await questionMetadataRepository.GetByIdAsync(subQuestionMetadataId);

                questionDetailsRepository.SoftDelete(targetSubQuestionDetailsEntity);

                questionMetadataRepository.SoftDelete(subQuestionMetadataEntity);

                var docFiles = await _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .GetAll(q => q.QuestionId == subQuestionMetadataId)
                    .ToListAsync();

                if (docFiles.Any())
                {
                    _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .DeleteRange(docFiles);
                }
            }
            else
            {
                questionDetailsRepository.SoftDelete(targetSubQuestionDetailsEntity);
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Subquestiondetailshasbeendeletedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.SubQuestionDetailsDeletionFailed);
        }

        public async Task<ApiResponse> EditQuestionMetadataAsync(QuestionMetadataAdditionOrUpdateDto questionMetaData)
        {
            var validatorResult = _questionValidatorService.ValidateAddedOrUpdatedQuestionMetadata(questionMetaData);

            if (validatorResult.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return validatorResult;
            }

            var trimmedQuestionCode = questionMetaData.Code.Trim();

            var organizationId = _filterParamsValues.OrganizationId;

            bool codeExists = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(q => q.Code.Trim() == trimmedQuestionCode && q.Id != questionMetaData.Id && q.OrganizationId == organizationId);

            if (codeExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.TheCodeAlreadyExistsInThisOrganizationPleaseWriteAnotherCode
                );
            }

            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == questionMetaData.Id, Including: nameof(FileUploadResponseSettings));

            if (questionMetadataEntity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    $"{Resource.SubquestionwithMetadataId} {questionMetaData.Id} {Resource.NotFound}"
                );
            }

            var allUserGroupIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .Select(x => x.GroupId)
                .ToList();

            var hasEditorAccess = await _commonService
                ._unitOfWork
                .Repository<ItemBankGroups, long>()
                .IsExistAsync(x =>
                    x.ItemBankId == questionMetadataEntity.ItemBankId &&
                    allUserGroupIds.Contains(x.OESGroupId) &&
                    !x.IsDeleted
                );

            bool canUseItemBank = await _itemBankAuthorizationService.CanUseItemBankAsync(questionMetadataEntity.ItemBankId);

            if (!canUseItemBank && !hasEditorAccess)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.NotAuthorizedToEditThisItemBank
                );
            }

            bool itemBankChanged = questionMetadataEntity.ItemBankId != questionMetaData.ItemBankId;
            bool newUnscored = itemBankChanged ? await GetItemBankUnscoredAsync(questionMetaData.ItemBankId) : questionMetadataEntity.Unscored;

            questionMetadataEntity.Code = trimmedQuestionCode;
            questionMetadataEntity.SubjectId = questionMetaData.QuestionSubjectId;
            questionMetadataEntity.QuestionTypeId = questionMetaData.QuestionTypeId;
            questionMetadataEntity.QuestionCategoryId = questionMetaData.QuestionCategoryId;
            questionMetadataEntity.DifficultyProfileId = questionMetaData.DifficultyProfileId;
            questionMetadataEntity.DifficultyLevelId = questionMetaData.DifficultyLevelId;
            questionMetadataEntity.Delta = questionMetaData.Delta;
            questionMetadataEntity.MaximumAnswerTime = questionMetaData.MaximumAnswerTime;
            questionMetadataEntity.ItemBankId = questionMetaData.ItemBankId;
            questionMetadataEntity.IloId = questionMetaData.IloId;
            questionMetadataEntity.QuestionsExhaustionCount = questionMetaData.QuestionsExhaustionCount;
            questionMetadataEntity.ScientificEditorPanelEnabled = questionMetaData.ScientificEditorPanelEnabled;
            questionMetadataEntity.FileManagerEditorPanelEnabled = questionMetaData.FileManagerEditorPanelEnabled;
            questionMetadataEntity.Unscored = newUnscored;

            if (questionMetadataEntity.QuestionTypeId == (long)Helper.Enums.QuestionType.FileUploadResponse)
            {
                var dtoSettings = questionMetaData.FileUploadResponseSettings;

                if (questionMetadataEntity.FileUploadResponseSettings == null)
                {
                    questionMetadataEntity.FileUploadResponseSettings = new FileUploadResponseSettings
                    {
                        QuestionMetadataId = questionMetadataEntity.Id
                    };

                    await _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .AddAsync(questionMetadataEntity.FileUploadResponseSettings);
                }

                questionMetadataEntity.FileUploadResponseSettings.Id = dtoSettings.Id ?? questionMetadataEntity.FileUploadResponseSettings.Id;
                questionMetadataEntity.FileUploadResponseSettings.ShowAnswerTextArea = dtoSettings.ShowAnswerTextArea;
                questionMetadataEntity.FileUploadResponseSettings.SupportedFileExtensions = dtoSettings.SupportedFileExtensions;
                questionMetadataEntity.FileUploadResponseSettings.UploadedFilesCount = dtoSettings.UploadedFilesCount;
                questionMetadataEntity.FileUploadResponseSettings.SingleFileMaxSizeInMB = dtoSettings.SingleFileMaxSizeInMB;
            }
            else
            {
                if (questionMetadataEntity.FileUploadResponseSettings != null)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<FileUploadResponseSettings, long>()
                        .Delete(questionMetadataEntity.FileUploadResponseSettings);
                }
            }
            var questionStatus = questionMetadataEntity.QuestionStatus;

            if (questionStatus == QuestionStatus.ReturnToEdit)
            {
                questionMetadataEntity.QuestionStatus = QuestionStatus.LayoutSelectedAndPending;
            }

            var subQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q => q.ParentId == questionMetadataEntity.Id)
                .ToListAsync();

            foreach (var subQuestion in subQuestions)
            {
                subQuestion.SubjectId = questionMetaData.QuestionSubjectId;
                subQuestion.QuestionCategoryId = questionMetaData.QuestionCategoryId;
                subQuestion.DifficultyProfileId = questionMetaData.DifficultyProfileId;
                subQuestion.DifficultyLevelId = questionMetaData.DifficultyLevelId;

                if (questionMetadataEntity.QuestionTypeId != (long)Helper.Enums.QuestionType.Comprehension)
                {
                    subQuestion.Delta = questionMetaData.Delta;
                }

                subQuestion.MaximumAnswerTime = questionMetaData.MaximumAnswerTime;
                subQuestion.ItemBankId = questionMetaData.ItemBankId;
                subQuestion.IloId = questionMetaData.IloId;
                subQuestion.QuestionsExhaustionCount = questionMetaData.QuestionsExhaustionCount;
                subQuestion.ScientificEditorPanelEnabled = questionMetaData.ScientificEditorPanelEnabled;
                subQuestion.FileManagerEditorPanelEnabled = questionMetaData.FileManagerEditorPanelEnabled;
                subQuestion.Unscored = newUnscored;

                if (subQuestion.QuestionStatus == QuestionStatus.ReturnToEdit)
                {
                    subQuestion.QuestionStatus = QuestionStatus.LayoutSelectedAndPending;
                }
            }

            var groupRepo = _commonService._unitOfWork.Repository<QuestionGroups, long>();

            var oldGroups = await groupRepo.GetAllAsync(
                x => x.QuestionId == questionMetadataEntity.Id,
                Including: nameof(QuestionGroups.OESGroup)
            );

            var existingGroupIds = oldGroups
                .Select(g => g.OESGroupId)
                .ToHashSet();

            var ownerGroup = oldGroups.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser);

            if (ownerGroup == null)
            {
                ownerGroup = await groupRepo.GetObjAsync(
                    g => g.QuestionId == questionMetadataEntity.Id && g.OESGroup.AutoCreatedForUser,
                    Including: nameof(QuestionGroups.OESGroup)
                );
            }

            if (questionMetaData.OESGroupDtos != null)
            {
                foreach (var g in oldGroups)
                {
                    if (ownerGroup != null && g.OESGroupId == ownerGroup.OESGroupId)
                        continue;

                    bool stillExists = questionMetaData
                        .OESGroupDtos
                        .Any(dto => dto.Id == g.OESGroupId);

                    if (!stillExists)
                    {
                        groupRepo.Delete(g);
                        existingGroupIds.Remove(g.OESGroupId);
                    }
                }

                foreach (var g in questionMetaData.OESGroupDtos.Where(x => !x.AutoCreatedForUser))
                {
                    if (!existingGroupIds.Contains(g.Id))
                    {
                        await groupRepo.AddAsync(new QuestionGroups
                        {
                            QuestionId = questionMetadataEntity.Id,
                            OESGroupId = g.Id
                        });

                        existingGroupIds.Add(g.Id);
                    }
                }
            }

            if (questionMetaData.OESGroupDtos != null)
            {
                foreach (var g in questionMetaData.OESGroupDtos.Where(x => !x.AutoCreatedForUser))
                {
                    if (!existingGroupIds.Contains(g.Id))
                    {
                        await groupRepo.AddAsync(new QuestionGroups
                        {
                            QuestionId = questionMetadataEntity.Id,
                            OESGroupId = g.Id
                        });

                        existingGroupIds.Add(g.Id);
                    }
                }
            }

            var result = await _commonService._unitOfWork.Complete();

            if (result >= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.QuestionUpdatedSuccessfully);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.SomethingWentWrong);
            }
        }

        public async Task<ApiResponse> ChangeQuestionCreationStatusAsync(UpdateQuestionCreationStatusRequestDto updateQuestionCreationStatusRequestDto)
        {
            var question = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(q => q.Id == updateQuestionCreationStatusRequestDto.QuestionMetadataId);

            if (question == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionFound
                );
            }

            question.QuestionStatus = updateQuestionCreationStatusRequestDto.CurrentQuestionStatus;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionCreationStatus
            );
        }

        public async Task<ApiResponse> ChangeQuestionsCreationStatusAsync(UpdateQuestionsCreationStatusRequestDto updateQuestionsCreationStatusRequestDto)
        {
            if (updateQuestionsCreationStatusRequestDto.QuestionCodes == null || updateQuestionsCreationStatusRequestDto.QuestionCodes.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.PleaseSelectAtLeastOneQuestion
                );
            }

            var affectedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(q => updateQuestionsCreationStatusRequestDto.QuestionCodes.Contains(q.Code))
                .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(q => q.QuestionStatus, updateQuestionsCreationStatusRequestDto.CurrentQuestionStatus)
                );

            if (affectedQuestions == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionFound
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionCreationStatus
            );
        }

        public async Task<ApiResponse> GetAllQuestionByItemBankId(List<long> itemBankIds)
        {
            if (itemBankIds == null || !itemBankIds.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.ItemBankIdsCannotBeEmpty
                );
            }

            // Fetch questions that belong to the selected item banks
            var questions = await _commonService.
                            _unitOfWork
                            .Repository<QuestionMetadata, long>()
                            .GetAll(q => itemBankIds.Contains(q.ItemBankId),
                                    Including: "QuestionDetails")
                            .Select(q => new ItemBankQuestionDto
                            {
                                Id = q.Id,
                                Code = q.Code,
                                Body = q.QuestionDetails.Select(d => d.Body).FirstOrDefault(),
                                Type = q.QuestionType.Name,
                                ItemBankId = q.ItemBankId
                            })
                            .ToListAsync();

            if (!questions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionsFoundForTheProvidedItemBankIds
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfully,
                questions
            );
        }

        public async Task<ApiResponse> GetAllQuestionsByIds(List<long> questionIds)
        {
            if (questionIds == null || questionIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.QuestionIdscannotbeempty
                );
            }

            var questions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q => questionIds.Contains(q.Id) &&
                             q.IsRoot &&
                             (q.ParentId == null || q.ParentId == 0),
                             Including: "QuestionDetails")
                .Select(q => new QuestionMetadataDto
                {
                    Id = q.Id,
                    Code = q.Code,
                    Body = q.QuestionDetails.Select(d => d.Body).FirstOrDefault(),
                    DifficultyLevelId = q.DifficultyLevelId,
                    QuestionTypeId = q.QuestionTypeId,
                    ParentId = q.ParentId,
                    IsRoot = q.IsRoot,
                    MaximumAnswerTime = q.MaximumAnswerTime,
                    ItemBankId = q.ItemBankId,
                    CurrentExhaustionCount = q.CurrentExhaustionCount,
                    QuestionsExhaustionCount = q.QuestionsExhaustionCount,
                    QuestionStatus = q.QuestionStatus
                })
                .ToListAsync();

            if (questions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoValidQuestionsFound
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfully,
                questions
            );
        }

        public async Task<ApiResponse> GetQuestionTypeCountsByItemBankId(long itemBankId)
        {
            var questionTypeCounts = await _commonService
                ._unitOfWork
                .Repository<QuestionTypeCountView, long>()
                .GetAllAsync(q => q.ItemBankId == itemBankId);

            if (!questionTypeCounts.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionsFoundForTheProvidedItemBankIds
                );
            }

            var result = questionTypeCounts
                .GroupBy(qtc => qtc.QuestionType)
                .Select(group => new QuestionTypeCountDto
                {
                    QuestionType = group.Key,
                    Difficulties = [.. group
                        .Select(qtc => new DifficultyCountDto
                        {
                            DifficultyLevel = qtc.DifficultyLevel,
                            Count = qtc.QuestionCount
                        })
                    ]
                })
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                new ItemBankQuestionCountDto
                {
                    ItemBankId = itemBankId,
                    QuestionTypes = result
                }
            );
        }

        public async Task<ApiResponse> GetNextQuestionForQualityCheckAsync(long currentId)
        {
            var currentQuestion = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.Id == currentId)
                .Select(x => new { x.Id, x.ItemBankId })
                .FirstOrDefaultAsync();

            if (currentQuestion is null || currentQuestion.ItemBankId <= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.Nomatchingrecordsfound,
                                    null);
            }

            // Try to get the next question with a higher ID in the same branch
            var nextQuestionId = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.ItemBankId == currentQuestion.ItemBankId &&
                            x.Id > currentId &&
                            x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending &&
                            x.IsRoot &&
                            x.ParentId == null)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            // Wrap around: If no next found, get the first question from the beginning of the same branch
            if (nextQuestionId == 0)
            {
                nextQuestionId = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.ItemBankId == currentQuestion.ItemBankId &&
                                x.Id < currentId &&
                                x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending &&
                                x.IsRoot &&
                                x.ParentId == null)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            var questionNavigationDto = new QuestionNavigationDto
            {
                TargetId = nextQuestionId,
                Status = QuestionStatus.LayoutSelectedAndPending
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.ComputedNextQuestion,
                                questionNavigationDto);
        }

        public async Task<ApiResponse> GetPreviousQuestionForQualityCheckAsync(long currentId)
        {
            var currentQuestion = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.Id == currentId)
                .Select(x => new { x.Id, x.ItemBankId })
                .FirstOrDefaultAsync();

            if (currentQuestion is null || currentQuestion.ItemBankId <= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.Nomatchingrecordsfound,
                                    null);
            }

            // Try to get the previous question with a lower ID in the same branch
            var previousQuestionId = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.ItemBankId == currentQuestion.ItemBankId &&
                            x.Id < currentId &&
                            x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending &&
                            x.IsRoot &&
                            x.ParentId == null)
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            // If no previous found, get the last question from the end of the same branch
            if (previousQuestionId == 0)
            {
                previousQuestionId = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.ItemBankId == currentQuestion.ItemBankId &&
                                x.Id > currentId &&
                                x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending &&
                                x.IsRoot &&
                                x.ParentId == null)
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            }

            var questionNavigationDto = new QuestionNavigationDto
            {
                TargetId = previousQuestionId,
                Status = QuestionStatus.LayoutSelectedAndPending
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.ComputedPreviousQuestion,
                                questionNavigationDto);
        }

        public async Task<ApiResponse> GetNextQuestionIdInSameBranchAsync(long currentId)
        {
            var currentQuestionItemBankId = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.Id == currentId)
                .Select(x => x.ItemBankId)
                .FirstOrDefaultAsync();

            if (currentQuestionItemBankId <= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.Nomatchingrecordsfound,
                                    null);
            }

            var nextQuestionId = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.Id != currentId &&
                            x.ItemBankId == currentQuestionItemBankId &&
                            x.QuestionStatus == QuestionStatus.LayoutSelectedAndPending &&
                            x.IsRoot && x.ParentId == null
                )
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var questionNavigationDto = new QuestionNavigationDto
            {
                TargetId = nextQuestionId,
                Status = QuestionStatus.LayoutSelectedAndPending
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.Success,
                                questionNavigationDto);
        }

        public async Task<ApiResponse> ValidateQuestionsByQuestionCodesAsync(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto)
        {
            var validationResult = await ValidateReceivedExcelSheetQuestionsAsync(addMultipleQuestionsRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message,
                    validationResult.ProblematicQuestionsCodes
                );
            }

            var questionIds = validationResult.ValidateExcelSheetQuestionsDto.QuestionWithSectionNameDtos.ConvertAll(q => q.QuestionId);

            var questionsItemBankIds = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q => questionIds.Contains(q.Id))
                .AsNoTracking()
                .Select(q => q.ItemBankId)
                .Distinct()
                .ToListAsync();

            var resultedDto = new QuestionIdsAndItemBankIdsDto
            {
                QuestionIds = questionIds,
                ItemBankIds = questionsItemBankIds,
                ValidateExcelSheetQuestionsDto = validationResult.ValidateExcelSheetQuestionsDto
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Validationcompletedsuccessfully,
                resultedDto
            );
        }

        public async Task<ApiResponse> GetQuestionsByItemBankAndType(MixedSelectedQuestionsNodeDto autoSelectedQuestionsNodeDto)
        {
            if (autoSelectedQuestionsNodeDto?.ItemBankId == null || autoSelectedQuestionsNodeDto.ItemBankId < 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.ItemBankNotFound
                );
            }

            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q => q.ItemBankId == autoSelectedQuestionsNodeDto.ItemBankId &&
                             q.QuestionTypeId == autoSelectedQuestionsNodeDto.QuestionTypeId &&
                             q.QuestionStatus == QuestionStatus.Approved &&
                             q.IsDeleted == false &&
                             q.IsActive == true &&
                             q.IsRoot &&
                             q.DifficultyLevel.DifficultyProfileId == autoSelectedQuestionsNodeDto.DifficultyProfileId &&
                             q.QuestionDetails.Any(qd => qd.LanguageId == autoSelectedQuestionsNodeDto.LanguageId),
                             Including: $"{nameof(QuestionMetadata.QuestionDetails)},{nameof(QuestionMetadata.SubQuestions)}");

            var questionDtos = await query
                .Select(q => new QuestionSimpleDataDto
                {
                    Id = q.Id,
                    Code = q.Code,
                    Body = q.QuestionDetails.FirstOrDefault(d => d.LanguageId == autoSelectedQuestionsNodeDto.LanguageId).Body,
                    DifficultyLevelId = q.DifficultyLevelId,
                    DifficultyLevelName = q.DifficultyLevel.Name,
                    SubQuestionsCount = autoSelectedQuestionsNodeDto.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension
                        ? q.SubQuestions.Count(s => !s.IsDeleted && s.IsActive)
                        : 1
                })
                .ToListAsync();

            questionDtos = [.. questionDtos
                .GroupBy(q => q.DifficultyLevelName)
                .Where(g =>
                {
                    if (!autoSelectedQuestionsNodeDto.DifficultyLevelsBreakdown.ContainsKey(g.Key))
                        return false;

                    var required = autoSelectedQuestionsNodeDto.DifficultyLevelsBreakdown[g.Key];

                    var available = autoSelectedQuestionsNodeDto.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension
                        ? g.Sum(x => x.SubQuestionsCount)
                        : g.Count();

                    return available >= required;
                })
                .SelectMany(g => g)];

            var result = new MixedSelectedQuestionsNodeDto
            {
                ItemBankId = autoSelectedQuestionsNodeDto.ItemBankId,
                ItemBankName = autoSelectedQuestionsNodeDto.ItemBankName,
                QuestionTypeId = autoSelectedQuestionsNodeDto.QuestionTypeId,
                QuestionTypeName = autoSelectedQuestionsNodeDto.QuestionTypeName,
                SectionName = autoSelectedQuestionsNodeDto.SectionName,
                CurrentQuestionsCount = autoSelectedQuestionsNodeDto.CurrentQuestionsCount,
                DifficultyLevelsBreakdown = autoSelectedQuestionsNodeDto.DifficultyLevelsBreakdown,
                AssignedDifficultyLevelsBreakdown = autoSelectedQuestionsNodeDto.AssignedDifficultyLevelsBreakdown,
                TotalManualQuestions = questionDtos,
                SubQuestionDistributions = autoSelectedQuestionsNodeDto.SubQuestionDistributions,
                LanguageId = autoSelectedQuestionsNodeDto.LanguageId
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfully,
                result
            );
        }

        public async Task<ApiResponse> GetAllQuestionWithGroupsAsync()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLower() ?? "System";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsTemplate &&
                !g.AutoCreatedForUser &&
                !g.IsPredefined &&
                g.GroupResources.Any() &&
                g.GroupResources.Any(r =>
                    !r.IsDeleted &&
                    r.ResourceType == ResourceType.Questions
                ) &&
                (
                    g.CreationUser.ToLower() == currentUser ||
                    g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
                ),
                Including: "GroupResources"
            );

            var groupDtos = groups
                .Select(g => new GetOESGroupDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                groupDtos
            );
        }

        public async Task<ApiResponse> AddOrUpdateMatchingPairsQuestionAsync(AddOrUpdateMatchingPairsRequestDto addOrUpdateMatchingPairsRequestDto)
        {
            var matchingPairsValidationResult = await ValidateMatchingPairsAsync(addOrUpdateMatchingPairsRequestDto);

            if (!matchingPairsValidationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    matchingPairsValidationResult.Message
                );
            }

            var incomingParentDetail = addOrUpdateMatchingPairsRequestDto.QuestionDetails.First();
            var languageId = incomingParentDetail.LanguageId;
            var existingDetailsId = incomingParentDetail.Id;

            var metadata = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetByIdAsync(addOrUpdateMatchingPairsRequestDto.MetadataParentId);

            if (metadata != null && metadata.QuestionStatus == QuestionStatus.Approved)
            {
                var existingDetails = await _commonService._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .Query()
                    .Include(qd => qd.MatchingPairQuestionItems)
                    .FirstOrDefaultAsync(qd => qd.QuestionMetadataId == addOrUpdateMatchingPairsRequestDto.MetadataParentId && qd.LanguageId == languageId);

                if (existingDetails != null && HasMatchingPairsChanged(existingDetails, addOrUpdateMatchingPairsRequestDto))
                {
                    await CreateQuestionVersion(existingDetails);
                    metadata.IsReplaced = true;
                }
            }

            await DeleteExistingMatchingPairsWithDragDropAsync(addOrUpdateMatchingPairsRequestDto.MetadataParentId, existingDetailsId);

            var questionDetailEntities = addOrUpdateMatchingPairsRequestDto.QuestionDetails.ConvertAll(dto =>
            {
                var body = dto.UseArabicNumbers
                    ? ConvertDigitsToArabicInText(dto.Body)
                    : dto.Body;

                var instructions = dto.UseArabicNumbers
                    ? ConvertDigitsToArabicInText(dto.Instructions)
                    : dto.Instructions;

                return new QuestionDetails
                {
                    Body = body,
                    Instructions = instructions,
                    ModelAnswer = null,
                    QuestionMetadataId = addOrUpdateMatchingPairsRequestDto.MetadataParentId,
                    LanguageId = dto.LanguageId,
                    UseArabicNumbers = dto.UseArabicNumbers,
                    HasShuffled = dto.HasShuffled,
                    MaxWords = dto.MaxWords,
                    MaxRecordingTimeInSeconds = dto.MaxRecordingTimeInSeconds
                };
            });

            await _commonService._unitOfWork
                .Repository<QuestionDetails, long>()
                .AddRangeAsync(questionDetailEntities);

            await _commonService._unitOfWork.Complete();

            var firstDetail = questionDetailEntities.First();

            var allMatchingItemEntities = addOrUpdateMatchingPairsRequestDto.MatchingItems.ConvertAll(item => new MatchingPairQuestionItems
            {
                Body = item.Body,
                ColumnOrder = item.ColumnOrder,
                QuestionDetailsId = firstDetail.Id,
                IsDataSource = item.IsDataSource
            });

            await _commonService._unitOfWork
                .Repository<MatchingPairQuestionItems, long>()
                .AddRangeAsync(allMatchingItemEntities);

            await _commonService._unitOfWork.Complete();

            var idMapping = addOrUpdateMatchingPairsRequestDto.MatchingItems
                .Zip(allMatchingItemEntities)
                .ToDictionary(pair => pair.First.Id, pair => pair.Second.Id);

            foreach (var (detail, dto) in questionDetailEntities.Zip(addOrUpdateMatchingPairsRequestDto.QuestionDetails))
            {
                if (string.IsNullOrWhiteSpace(dto.ModelAnswer)) continue;

                var clientModelAnswer = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(dto.ModelAnswer);
                if (clientModelAnswer == null) continue;

                var resolvedModelAnswer = new List<MatchingPairModelAnswerDto>();

                foreach (var item in clientModelAnswer)
                {
                    if (idMapping.TryGetValue(item.QuestionItemId, out var realQuestionId))
                    {
                        var resolvedAnswerIds = new List<long>();
                        foreach (var ansId in item.AnswerIds)
                        {
                            if (idMapping.TryGetValue(ansId, out var realAnswerId))
                            {
                                resolvedAnswerIds.Add(realAnswerId);
                            }
                        }

                        if (resolvedAnswerIds.Count > 0)
                        {
                            resolvedModelAnswer.Add(new MatchingPairModelAnswerDto
                            {
                                QuestionItemId = realQuestionId,
                                AnswerIds = resolvedAnswerIds
                            });
                        }
                    }
                }

                detail.ModelAnswer = JsonSerializer.Serialize(resolvedModelAnswer);
            }

            await _commonService._unitOfWork.Complete();

            var allMediaEntities = _htmlHelperService
                .ExtractMediaEntitiesFromQuestionDetails(questionDetailEntities);

            if (allMediaEntities.Count > 0)
            {
                await _commonService._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangeAsync(allMediaEntities);

                await _commonService._unitOfWork.Complete();
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionDetailsAddedSuccessfully
            );
        }

        public async Task<ApiResponse> GetMatchingPairsQuestionAsync(long metadataParentId, long? languageId)
        {
            var parentExists = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(x => x.Id == metadataParentId && x.IsRoot);

            if (!parentExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.MatchingRootQuestionNotFound
                );
            }

            var details = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAll(d => d.QuestionMetadataId == metadataParentId && (!languageId.HasValue || d.LanguageId == languageId.Value))
                .ToListAsync();

            var questionDetailsList = details.ConvertAll(d => new QuestionDetailsDto
            {
                Id = d.Id,
                Body = d.Body,
                Instructions = d.Instructions,
                ModelAnswer = d.ModelAnswer,
                QuestionMetadataId = d.QuestionMetadataId,
                LanguageId = d.LanguageId,
                UseArabicNumbers = d.UseArabicNumbers,
                HasShuffled = d.HasShuffled,
                MaxWords = d.MaxWords,
                MaxRecordingTimeInSeconds = d.MaxRecordingTimeInSeconds,
                AttachmentFileName = d.AttachmentFileName
            });

            var matchingItems = await _commonService
                ._unitOfWork
                .Repository<MatchingPairQuestionItems, long>()
                .GetAll(m => m.QuestionDetails.QuestionMetadataId == metadataParentId && (!languageId.HasValue || m.QuestionDetails.LanguageId == languageId.Value))
                .OrderBy(m => m.ColumnOrder)
                .ThenBy(m => m.Id)
                .ToListAsync();

            var matchingItemDtos = matchingItems.ConvertAll(m => new MatchingPairQuestionItemDto
            {
                Id = m.Id,
                Body = m.Body,
                ColumnOrder = m.ColumnOrder,
                QuestionDetailsId = m.QuestionDetailsId,
                IsDataSource = m.IsDataSource
            });

            var result = new GetMatchingPairsResponseDto(
                metadataParentId,
                questionDetailsList,
                matchingItemDtos
            );

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                result
            );
        }

        public async Task<ApiResponse> GetSegmentQuestionByMetaDataIdAsync(long parentId, long languageId)
        {
            var segmentQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == parentId || qm.ParentId == parentId)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.SegmentQuestionProperties)
                .Include(qm => qm.FormQuestions)
                .Include(qm => qm.BlocksQuestions)
                .Include(qm => qm.ManualPaperItemBankQuestionSections)
                .AsNoTracking()
                .ToListAsync();

            if (segmentQuestions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CannotFindQuestionMetadataWithTheGivenId);
            }

            var parentMetadata = segmentQuestions.FirstOrDefault(x => x.Id == parentId);

            bool isParentUsed = false;

            var segmentQuestionDtos = segmentQuestions
                .Select(questionMetadata =>
                {
                    var questionDetails = questionMetadata.QuestionDetails.FirstOrDefault(q => q.LanguageId == languageId);
                    var dto = questionDetails is null ? null : MapToSegmentQuestionDto(questionMetadata, questionDetails);
                    if (dto != null) dto.SegmentQuestionDetailsDto.IsUsedInExam = isParentUsed;
                    return dto;
                })
                .Where(x => x is not null)
                .OrderBy(x => x.SegmentQuestionConfigDto?.OrderNumber)
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfully,
                segmentQuestionDtos
            );
        }

        public async Task<ApiResponse> DeleteSegmentQuestionLanguageVariantAsync(long segmentQuestionMetadataId, long segmentQuestionDetailsId)
        {
            var questionMetadataRepository = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>();

            var questionDetailsRepository = _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>();

            var segmentQuestionPropertiesRepository = _commonService
                ._unitOfWork
                .Repository<SegmentQuestionProperties, long>();

            var questionDetailsEntities = (await questionDetailsRepository
                .GetAllAsync(qd => qd.QuestionMetadataId == segmentQuestionMetadataId))
                .ToList();

            if (questionDetailsEntities.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SegmentQuestionDetailsDeletionFailed);
            }

            // Find the specific segment question details to delete
            var targetSegmentQuestionDetailsEntity = questionDetailsEntities.Find(qd => qd.Id == segmentQuestionDetailsId);

            if (targetSegmentQuestionDetailsEntity is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SegmentQuestionDetailsDeletionFailed);
            }

            var segmentSubMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetByIdAsync(segmentQuestionMetadataId);

            if (segmentSubMetadata != null && segmentSubMetadata.QuestionStatus == QuestionStatus.Approved)
            {
                long rootParentId = segmentSubMetadata.IsRoot
                    ? segmentSubMetadata.Id
                    : (segmentSubMetadata.ParentId ?? segmentSubMetadata.Id);

                var rootMetadata = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .GetByIdAsync(rootParentId);

                if (rootMetadata?.QuestionStatus == QuestionStatus.Approved)
                {
                    await CreateSegmentQuestionVersionsAsync(rootParentId, targetSegmentQuestionDetailsEntity.LanguageId);
                    rootMetadata.IsReplaced = true;
                }
            }

            // If this is the only language variant, delete the metadata and all related data
            if (questionDetailsEntities.Count == 1)
            {
                var segmentQuestionMetadataEntity = await questionMetadataRepository.GetByIdAsync(segmentQuestionMetadataId);

                // Delete segment question properties first
                var segmentProperties = await segmentQuestionPropertiesRepository.GetObjAsync(sp => sp.Id == targetSegmentQuestionDetailsEntity.Id);

                if (segmentProperties is not null)
                {
                    segmentQuestionPropertiesRepository.SoftDelete(segmentProperties);
                }

                // Delete question details
                questionDetailsRepository.SoftDelete(targetSegmentQuestionDetailsEntity);

                if (!segmentQuestionMetadataEntity.IsRoot)
                {
                    // Delete metadata
                    questionMetadataRepository.SoftDelete(segmentQuestionMetadataEntity);
                }

                // Delete related doc files
                var docFiles = await _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .GetAll(q => q.QuestionId == segmentQuestionMetadataId)
                    .ToListAsync();

                if (docFiles.Count > 0)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .DeleteRange(docFiles);
                }
            }
            else
            {
                var segmentProperties = await segmentQuestionPropertiesRepository.GetObjAsync(sp => sp.Id == targetSegmentQuestionDetailsEntity.Id);

                if (segmentProperties is not null)
                {
                    segmentQuestionPropertiesRepository.SoftDelete(segmentProperties);
                }

                questionDetailsRepository.SoftDelete(targetSegmentQuestionDetailsEntity);
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.SegmentQuestionDetailsDeletionSuccessful);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.InternalServerError,
                                Resource.SegmentQuestionDetailsDeletionFailed);
        }

        public async Task<ApiResponse> AddOrUpdateSegmentQuestionAsync(List<SegmentQuestionDto> segmentQuestionDto)
        {
            if (segmentQuestionDto.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.SegmentQuestionDataIsNull
                );
            }

            var parentMetadataId = segmentQuestionDto[0].SegmentQuestionDetailsDto.QuestionMetadataId;

            var parentMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == parentMetadataId);

            if (parentMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.ParentMetadataIsNull
                );
            }

            var existingMetadataList = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(x => (x.Id == parentMetadata.Id || x.ParentId == parentMetadata.Id) && x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (segmentQuestionDto.Count > existingMetadataList.Count)
            {
                int neededCopies = segmentQuestionDto.Count - existingMetadataList.Count;

                var creationResult = await CreateSegmentSubQuestionMetadataCopiesAsync(parentMetadata, neededCopies);

                if (creationResult.CustomCodeStatus != CustomCodeStatus.Success)
                {
                    return creationResult;
                }

                existingMetadataList.AddRange((List<QuestionMetadata>)creationResult.Data ?? []);
            }

            var existingMetadataIds = existingMetadataList.ConvertAll(x => x.Id);

            var existingDetails = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .Include(x => x.SegmentQuestionProperties)
                .Where(x => existingMetadataIds.Contains(x.QuestionMetadataId))
                .ToListAsync();

            if (parentMetadata.QuestionStatus == QuestionStatus.Approved)
            {
                var existingByMetadataId = existingDetails.ToDictionary(x => x.QuestionMetadataId);

                bool anyChanged = segmentQuestionDto.Count > existingByMetadataId.Count;

                if (!anyChanged)
                {
                    for (int i = 0; i < segmentQuestionDto.Count && i < existingMetadataList.Count; i++)
                    {
                        var metadataId = existingMetadataList[i].Id;
                        if (existingByMetadataId.TryGetValue(metadataId, out var existingDetail) &&
                            HasSegmentQuestionDetailsChanged(existingDetail, segmentQuestionDto[i]))
                        {
                            anyChanged = true;
                            break;
                        }
                    }
                }

                if (anyChanged)
                {
                    var languageId = segmentQuestionDto[0].SegmentQuestionDetailsDto.LanguageId;
                    await CreateSegmentQuestionVersionsAsync(parentMetadataId, languageId);
                    parentMetadata.IsReplaced = true;
                }
            }

            var (questionsToInsert, questionsToUpdate) = AddSegmentQuestionDetails(
                segmentQuestionDto,
                existingMetadataList,
                existingDetails
            );

            if (questionsToInsert.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .AddRangAsync(questionsToInsert);
            }

            if (questionsToUpdate.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .UpdateRange(questionsToUpdate);
            }

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                await SaveSegmentQuestionMediaAsync(segmentQuestionDto);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK
                );
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError
                );
            }
        }

        public async Task<ApiResponse> GetQuestionGroupsAsync(long questionId)
        {
            var questionGroups = await _commonService._unitOfWork
                .Repository<QuestionGroups, long>()
                .GetAllAsync(x =>
                    x.QuestionId == questionId &&
                    !x.OESGroup.IsTemplate);

            var dto = new QuestionGroupsDto();

            foreach (var qg in questionGroups)
                dto.GroupsIds.Add(qg.OESGroupId);

            dto.OwnerGroupId = questionGroups.FirstOrDefault()?.OESGroupId;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                dto
            );
        }

        public async Task<ApiResponse> GetQuestionAnalyticsIndicatorsAsync(PaginationSearchModel searchModel)
        {
            try
            {
                var filter = JsonSerializer.Deserialize<QuestionIndicatorFilterDto>(searchModel.FilterObj.ToString());

                var cachedData = await GetCachedQuestionIndicatorsAsync(filter);

                var query = cachedData.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchModel.SearchKey))
                {
                    query = query.Where(x => (x.QuestionCode ?? "").Contains(searchModel.SearchKey, StringComparison.OrdinalIgnoreCase));
                }

                var totalItems = query.Count();

                if (!searchModel.PaginationOff)
                {
                    query = query.Skip(searchModel.PageIndex * searchModel.PageSize).Take(searchModel.PageSize);
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new CustomTableData<QuestionAnalyticsIndicatorDto>([.. query], totalItems)
                );
            }
            catch (Exception ex)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    $"{Resource.Error}: {ex.Message}"
                );
            }
        }

        public async Task<ApiResponse> ExportQuestionIndicatorsAsync(QuestionIndicatorExportRequestDto request)
        {
            try
            {
                var filter = new QuestionIndicatorFilterDto
                {
                    Percentage = request.Percentage,
                    Type = request.Type,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                var data = await GetCachedQuestionIndicatorsAsync(filter);

                var columns = new List<ExcelExportHelper.ColumnDefinition<QuestionAnalyticsIndicatorDto>>
                {
                    new() { Header = Resource.QuestionCode, ValueSelector = x => x.QuestionCode },
                    new() { Header = Resource.TotalAnswered, ValueSelector = x => x.TotalAnswered },
                    new() { Header = Resource.CorrectCount, ValueSelector = x => x.CorrectCount },
                    new() { Header = Resource.WrongCount, ValueSelector = x => x.WrongCount },
                    new() { Header = Resource.Percentage, ValueSelector = x => x.Percentage }
                };

                var base64 = ExcelExportHelper.GenerateExcelBase64(data, "Report", columns);

                var from = request.FromDate?.ToString("yyyy-MM-dd") ?? "NA";
                var to = request.ToDate?.ToString("yyyy-MM-dd") ?? "NA";
                var fileName = $"{request.Type}_Question_report_{from}_to_{to}.xlsx";

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new DownloadFileDto(fileName, base64)
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

        public async Task<ApiResponse> AddOrUpdateMatchingPairsWithDragDropQuestionAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto request)
        {
            var validationResult = await ValidateMatchingPairsWithDragDropAsync(request);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var incomingParentDetail = request.QuestionDetails.First();
            var languageId = incomingParentDetail.LanguageId;
            var existingDetailsId = incomingParentDetail.Id;

            var metadata = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetByIdAsync(request.MetadataParentId);

            if (metadata != null && metadata.QuestionStatus == QuestionStatus.Approved)
            {
                var existingDetails = await _commonService._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .Query()
                    .Include(qd => qd.MatchingPairQuestionItems)
                    .FirstOrDefaultAsync(qd => qd.QuestionMetadataId == request.MetadataParentId && qd.LanguageId == languageId);

                if (existingDetails != null && HasMatchingPairsWithDragDropChanged(existingDetails, request))
                {
                    await CreateQuestionVersion(existingDetails);
                    metadata.IsReplaced = true;
                }
            }

            await DeleteExistingMatchingPairsWithDragDropAsync(request.MetadataParentId, existingDetailsId);

            var questionDetailEntities = request.QuestionDetails.ConvertAll(dto =>
            {
                var body = dto.UseArabicNumbers
                    ? ConvertDigitsToArabicInText(dto.Body)
                    : dto.Body;

                var instructions = dto.UseArabicNumbers
                    ? ConvertDigitsToArabicInText(dto.Instructions)
                    : dto.Instructions;

                return new QuestionDetails
                {
                    Body = body,
                    Instructions = instructions,
                    ModelAnswer = null,
                    QuestionMetadataId = request.MetadataParentId,
                    LanguageId = dto.LanguageId,
                    UseArabicNumbers = dto.UseArabicNumbers,
                    HasShuffled = dto.HasShuffled,
                    MaxWords = dto.MaxWords,
                    MaxRecordingTimeInSeconds = dto.MaxRecordingTimeInSeconds
                };
            });

            await _commonService._unitOfWork
                .Repository<QuestionDetails, long>()
                .AddRangeAsync(questionDetailEntities);

            await _commonService._unitOfWork.Complete();

            var firstDetail = questionDetailEntities.First();

            var allMatchingItemEntities = request.MatchingItems.ConvertAll(item => new MatchingPairQuestionItems
            {
                Body = item.Body,
                ColumnOrder = item.ColumnOrder,
                QuestionDetailsId = firstDetail.Id,
                IsDataSource = item.IsDataSource
            });

            await _commonService._unitOfWork
                .Repository<MatchingPairQuestionItems, long>()
                .AddRangeAsync(allMatchingItemEntities);

            await _commonService._unitOfWork.Complete();

            var idMapping = request.MatchingItems
                .Zip(allMatchingItemEntities)
                .ToDictionary(pair => pair.First.Id, pair => pair.Second.Id);

            foreach (var (detail, dto) in questionDetailEntities.Zip(request.QuestionDetails))
            {
                if (string.IsNullOrWhiteSpace(dto.ModelAnswer)) continue;

                var clientModelAnswer = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(dto.ModelAnswer);
                if (clientModelAnswer == null) continue;

                var resolvedModelAnswer = new List<MatchingPairModelAnswerDto>();

                foreach (var item in clientModelAnswer)
                {
                    if (idMapping.TryGetValue(item.QuestionItemId, out var realQuestionId))
                    {
                        var resolvedAnswerIds = new List<long>();
                        foreach (var ansId in item.AnswerIds)
                        {
                            if (idMapping.TryGetValue(ansId, out var realAnswerId))
                            {
                                resolvedAnswerIds.Add(realAnswerId);
                            }
                        }

                        if (resolvedAnswerIds.Count > 0)
                        {
                            resolvedModelAnswer.Add(new MatchingPairModelAnswerDto
                            {
                                QuestionItemId = realQuestionId,
                                AnswerIds = resolvedAnswerIds
                            });
                        }
                    }
                }
                detail.ModelAnswer = JsonSerializer.Serialize(resolvedModelAnswer);
            }

            await _commonService._unitOfWork.Complete();

            var allMediaEntities = _htmlHelperService
                .ExtractMediaEntitiesFromQuestionDetails(questionDetailEntities);

            if (allMediaEntities.Count > 0)
            {
                await _commonService._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangeAsync(allMediaEntities);

                await _commonService._unitOfWork.Complete();
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionDetailsAddedSuccessfully
            );
        }

        public async Task<ApiResponse> GetMatchingPairsWithDragDropQuestionAsync(long metadataParentId, long? languageId)
        {
            var parentExists = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(x => x.Id == metadataParentId && x.IsRoot);

            if (!parentExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.MatchingRootQuestionNotFound
                );
            }

            var details = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .GetAll(d => d.QuestionMetadataId == metadataParentId && (!languageId.HasValue || d.LanguageId == languageId.Value))
                .ToListAsync();

            var detailDtos = details.ConvertAll(d => new QuestionDetailsDto
            {
                Id = d.Id,
                Body = d.Body,
                Instructions = d.Instructions,
                ModelAnswer = d.ModelAnswer,
                QuestionMetadataId = d.QuestionMetadataId,
                LanguageId = d.LanguageId,
                UseArabicNumbers = d.UseArabicNumbers,
                HasShuffled = d.HasShuffled,
                MaxWords = d.MaxWords,
                MaxRecordingTimeInSeconds = d.MaxRecordingTimeInSeconds,
                AttachmentFileName = d.AttachmentFileName
            });

            var matchingItems = await _commonService
                ._unitOfWork
                .Repository<MatchingPairQuestionItems, long>()
                .GetAll(m => m.QuestionDetails.QuestionMetadataId == metadataParentId && (!languageId.HasValue || m.QuestionDetails.LanguageId == languageId.Value))
                .OrderBy(m => m.ColumnOrder)
                .ThenBy(m => m.Id)
                .ToListAsync();

            var matchingItemDtos = matchingItems.ConvertAll(m => new MatchingPairQuestionItemDto
            {
                Id = m.Id,
                Body = m.Body,
                ColumnOrder = m.ColumnOrder,
                QuestionDetailsId = m.QuestionDetailsId,
                IsDataSource = m.IsDataSource
            });

            var result = new GetMatchingPairsWithDragDropResponseDto(
                metadataParentId,
                detailDtos,
                matchingItemDtos
            );

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                result
            );
        }

        public async Task<ApiResponse> GetFilteredQuestionsAsync(PaginationSearchModel pagination)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var searchModel = pagination;
            QuestionAdvancedFilterDto filter = null;

            if (pagination.FilterObj is JsonElement outerJson)
            {
                var unwrapped = outerJson.Deserialize<PaginationSearchModel>(options);
                if (unwrapped != null)
                {
                    searchModel = unwrapped;
                    if (unwrapped.FilterObj is JsonElement filterJson)
                        filter = filterJson.Deserialize<QuestionAdvancedFilterDto>(options);
                }
            }

            var (data, totalRecords) = await RunFilteredQuestionsSpAsync(
                searchModel.SearchKey, filter, searchModel.FromDate, searchModel.ToDate,
                pagination.PageIndex, pagination.PageSize, pagination.PaginationOff);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                new CustomTableData<FilteredQuestionsDto>(data, totalRecords));
        }

        public async Task<ExcelFileResult> ExportFilteredQuestionsToExcelAsync(PaginationSearchModel pagination)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            QuestionAdvancedFilterDto filter = null;

            if (pagination.FilterObj is JsonElement filterJson)
                filter = filterJson.Deserialize<QuestionAdvancedFilterDto>(options);

            var (data, _) = await RunFilteredQuestionsSpAsync(
                pagination.SearchKey, filter, pagination.FromDate, pagination.ToDate,
                pageIndex: 0, pageSize: 0, paginationOff: true);

            var columns = new List<ExcelExportHelper.ColumnDefinition<FilteredQuestionsDto>>
            {
                new() { Header = Resource.QuestionCode,     ValueSelector = x => x.Code },
                new() { Header = Resource.Category,         ValueSelector = x => x.Category },
                new() { Header = Resource.QuestionTypeName, ValueSelector = x => x.QuestionTypeName },
                new() { Header = Resource.CreationDate,     ValueSelector = x => x.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss") },
                new() { Header = Resource.CreationUser,     ValueSelector = x => x.CreatedBy },
                new() { Header = Resource.ModificationDate, ValueSelector = x => x.ModifiedDate?.ToString("yyyy-MM-dd HH:mm:ss") },
                new() { Header = Resource.ModificationUser, ValueSelector = x => x.ModifiedBy },
            };

            return new ExcelFileResult(
                Bytes: ExcelExportHelper.GenerateExcelBytes(data, Resource.QuestionFilter, columns),
                FileName: $"FilteredQuestions_{DateTimeHelper.Now:yyyy-MM-dd}.xlsx",
                ContentType: MiscConstants.ExcelContentType);
        }

        private async Task<(List<FilteredQuestionsDto> Data, int TotalRecords)> RunFilteredQuestionsSpAsync(
            string searchKey, QuestionAdvancedFilterDto filter,
            DateTime? fromDate, DateTime? toDate,
            int pageIndex, int pageSize, bool paginationOff)
        {
            await using var command = _commonService._unitOfWork.CreateDbCommand();
            command.CommandText = nameof(GetFilteredQuestionsProcedure);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddRange(new[]
            {
                new MySqlParameter("@p_Signature",          MySqlDbType.VarChar)  { Value = _filterParamsValues.Signature ?? (object)DBNull.Value },
                new MySqlParameter("@p_SearchKey",          MySqlDbType.Text)     { Value = string.IsNullOrWhiteSpace(searchKey) ? DBNull.Value : searchKey.Trim() },
                new MySqlParameter("@p_CategoryId",         MySqlDbType.Int64)    { Value = filter?.CategoryId > 0 ? filter.CategoryId.Value : DBNull.Value },
                new MySqlParameter("@p_QuestionTypeId",     MySqlDbType.Int64)    { Value = filter?.QuestionTypeId > 0 ? filter.QuestionTypeId.Value : DBNull.Value },
                new MySqlParameter("@p_CreatedByUserName",  MySqlDbType.Text)     { Value = string.IsNullOrWhiteSpace(filter?.CreatedByUserName)  ? DBNull.Value : filter.CreatedByUserName.Trim() },
                new MySqlParameter("@p_ModifiedByUserName", MySqlDbType.Text)     { Value = string.IsNullOrWhiteSpace(filter?.ModifiedByUserName) ? DBNull.Value : filter.ModifiedByUserName.Trim() },
                new MySqlParameter("@p_FromDate",           MySqlDbType.DateTime) { Value = (object?)fromDate ?? DBNull.Value },
                new MySqlParameter("@p_ToDate",             MySqlDbType.DateTime) { Value = (object?)toDate ?? DBNull.Value },
                new MySqlParameter("@p_ModifiedFromDate",   MySqlDbType.DateTime) { Value = (object?)filter?.ModifiedFromDate ?? DBNull.Value },
                new MySqlParameter("@p_ModifiedToDate",     MySqlDbType.DateTime) { Value = (object?)filter?.ModifiedToDate ?? DBNull.Value },
                new MySqlParameter("@p_PageIndex",          MySqlDbType.Int32)    { Value = pageIndex },
                new MySqlParameter("@p_PageSize",           MySqlDbType.Int32)    { Value = pageSize > 0 ? pageSize : 10 },
                new MySqlParameter("@p_PaginationOff",      MySqlDbType.Bool)     { Value = paginationOff },
            });

            await _commonService._unitOfWork.OpenConnectionAsync();

            var data = new List<FilteredQuestionsDto>();
            int totalRecords = 0;

            await using var reader = await command.ExecuteReaderAsync();

            // First result set: total record count
            if (await reader.ReadAsync())
                totalRecords = reader.GetInt32("TotalRecords");

            // Second result set: the page data
            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                data.Add(new FilteredQuestionsDto
                {
                    Id = reader.GetInt64(nameof(FilteredQuestionsDto.Id)),
                    Code = reader.IsDBNull(nameof(FilteredQuestionsDto.Code)) ? null : reader.GetString(nameof(FilteredQuestionsDto.Code)),
                    Category = reader.IsDBNull(nameof(FilteredQuestionsDto.Category)) ? null : reader.GetString(nameof(FilteredQuestionsDto.Category)),
                    QuestionTypeName = reader.IsDBNull(nameof(FilteredQuestionsDto.QuestionTypeName)) ? null : reader.GetString(nameof(FilteredQuestionsDto.QuestionTypeName)),
                    CreatedDate = reader.IsDBNull(nameof(FilteredQuestionsDto.CreatedDate)) ? null : reader.GetDateTime(nameof(FilteredQuestionsDto.CreatedDate)),
                    CreatedBy = reader.IsDBNull(nameof(FilteredQuestionsDto.CreatedBy)) ? null : reader.GetString(nameof(FilteredQuestionsDto.CreatedBy)),
                    ModifiedDate = reader.IsDBNull(nameof(FilteredQuestionsDto.ModifiedDate)) ? null : reader.GetDateTime(nameof(FilteredQuestionsDto.ModifiedDate)),
                    ModifiedBy = reader.IsDBNull(nameof(FilteredQuestionsDto.ModifiedBy)) ? null : reader.GetString(nameof(FilteredQuestionsDto.ModifiedBy)),
                });
            }

            return (data, totalRecords);
        }

        public async Task<ApiResponse> GetQuestionAuthorsAsync()
        {
            var creators = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => (x.ParentId == null || x.ParentId == 0) && x.CreationUser != null)
                .AsNoTracking()
                .Select(x => x.CreationUser)
                .Distinct()
                .ToListAsync();

            var modifiers = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => (x.ParentId == null || x.ParentId == 0) && x.ModeficationUser != null)
                .AsNoTracking()
                .Select(x => x.ModeficationUser)
                .Distinct()
                .ToListAsync();

            // Superadmin users appear as blank in the dropdown — selecting blank filters by "superadmin"
            var authors = creators.Union(modifiers)
                .Select(x => x.ToLower().Contains("superadmin") ? nameof(System) : x)
                .Distinct()
                .Order()
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                authors
            );
        }

        public async Task<ApiResponse> SaveAIGeneratedQuestionsAsync(IReadOnlyList<AIQuestionMetadataDto> questions)
        {
            if (questions == null || questions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.QuestionMetadataCannotBeNull);
            }

            var invalidTypeCodes = questions
                .Where(q => !AllowedBulkCreationQuestionTypes.Contains(q.QuestionTypeId))
                .Select(q => q.Code)
                .ToList();

            if (invalidTypeCodes.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    $"{Resource.SomethingWentWrong}: {string.Join(", ", invalidTypeCodes)}",
                    invalidTypeCodes);
            }

            var duplicateCodesWithinRequest = questions
                .Select(q => q.Code?.Trim())
                .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCodesWithinRequest.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.TheCodeAlreadyExistsInThisOrganizationPleaseWriteAnotherCode,
                    duplicateCodesWithinRequest);
            }

            var organizationId = _filterParamsValues.OrganizationId;
            var requestedCodes = questions.Select(q => q.Code.Trim()).ToList();

            var existingCodes = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(q => requestedCodes.Contains(q.Code) && q.OrganizationId == organizationId)
                .Select(q => q.Code)
                .ToListAsync();

            if (existingCodes.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.TheCodeAlreadyExistsInThisOrganizationPleaseWriteAnotherCode,
                    existingCodes);
            }

            var distinctItemBankIds = questions
                .Select(q => q.ItemBankId)
                .Distinct()
                .ToList();

            var authorizedItemBankIds = new HashSet<long>();

            foreach (var itemBankId in distinctItemBankIds)
            {
                if (await _itemBankAuthorizationService.CanUseItemBankAsync(itemBankId))
                {
                    authorizedItemBankIds.Add(itemBankId);
                }
            }

            var unauthorizedCodes = questions
                .Where(q => !authorizedItemBankIds.Contains(q.ItemBankId))
                .Select(q => q.Code)
                .ToList();

            if (unauthorizedCodes.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.YouAreNotAuthorizedToUseThisItemBank,
                    unauthorizedCodes);
            }

            var itemBankUnscoredMap = new Dictionary<long, bool>();

            foreach (var itemBankId in distinctItemBankIds)
            {
                itemBankUnscoredMap[itemBankId] = await GetItemBankUnscoredAsync(itemBankId);
            }

            _ = Guid.TryParse(_filterParamsValues.UserId, out Guid parsedUserId);

            var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

            var metadataEntitiesToAdd = new List<QuestionMetadata>();

            foreach (var question in questions)
            {
                var questionDetailsEntities = question.Details.ConvertAll(detail => new QuestionDetails
                {
                    Body = detail.Body,
                    LanguageId = detail.LanguageId,
                    Instructions = detail.Instructions,
                    ModelAnswer = detail.ModelAnswer,
                    QuestionsChoices = detail.Choices.ConvertAll(choice => new QuestionsChoices
                    {
                        ChoiceText = choice.Text,
                        IsCorrectAnswer = choice.IsCorrect,
                        OrderId = choice.Order
                    })
                });

                var questionMetadataEntity = new QuestionMetadata
                {
                    Code = question.Code.Trim(),
                    SubjectId = question.SubjectId,
                    QuestionTypeId = question.QuestionTypeId,
                    QuestionCategoryId = question.CategoryId,
                    QuestionLayoutId = question.LayoutId,
                    DifficultyProfileId = question.DifficultyProfileId,
                    DifficultyLevelId = question.DifficultyLevelId,
                    IloId = question.IloId,
                    IsActive = true,
                    IsRoot = question.IsRoot,
                    ItemBankId = question.ItemBankId,
                    Author = question.Author,
                    Delta = question.Delta,
                    QuestionStatus = QuestionStatus.LayoutSelectedAndPending,
                    QuestionsExhaustionCount = question.QuestionsExhaustionCount,
                    ScientificEditorPanelEnabled = question.ScientificEditorPanelEnabled,
                    FileManagerEditorPanelEnabled = question.FileManagerEditorPanelEnabled,
                    Unscored = itemBankUnscoredMap[question.ItemBankId],
                    QuestionDetails = questionDetailsEntities
                };

                if (isSuperAdmin && question.OESGroupDtos?.Count > 0)
                {
                    questionMetadataEntity.QuestionGroups = [.. question
                        .OESGroupDtos
                        .Where(x => !x.AutoCreatedForUser)
                        .Select(x => new QuestionGroups
                        {
                            OESGroupId = x.Id
                        })];
                }

                metadataEntitiesToAdd.Add(questionMetadataEntity);
            }

            await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .AddRangeAsync(metadataEntitiesToAdd);

            var affectedRows = await _commonService._unitOfWork.Complete();

            if (affectedRows <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.SomethingWentWrong);
            }

            var allQuestionDetails = new List<QuestionDetails>();

            for (var i = 0; i < metadataEntitiesToAdd.Count; i++)
            {
                var metadataEntity = metadataEntitiesToAdd[i];
                var questionDto = questions[i];

                if (!isSuperAdmin)
                {
                    await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(
                            new AutoPermissionAssignmentRequest
                            {
                                EntityId = metadataEntity.Id,
                                EntityName = metadataEntity.Code,
                                ResourceType = ResourceType.Questions,
                                UserId = parsedUserId,
                                AdditionalGroupIds = questionDto.OESGroupDtos?.Select(x => x.Id).ToList(),
                                EntityGroupType = typeof(QuestionGroups)
                            });
                }

                allQuestionDetails.AddRange(metadataEntity.QuestionDetails);
            }

            var extractedFiles = _htmlHelperService.ExtractMediaEntitiesFromQuestionDetails(allQuestionDetails);

            if (extractedFiles.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangAsync(extractedFiles);

                await _commonService._unitOfWork.Complete();
            }

            var savedQuestionIds = metadataEntitiesToAdd.ConvertAll(m => m.Id);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsAddedSuccessfully,
                savedQuestionIds);
        }

        #region Helper Methods

        private async Task<List<QuestionAnalyticsIndicatorDto>> GetCachedQuestionIndicatorsAsync(QuestionIndicatorFilterDto filter)
        {
            // Create a consistent cache key based on filter values
            var cacheKey = $"QuestionIndicator_{filter.Percentage}_{filter.Type}_{filter.FromDate?.Ticks}_{filter.ToDate?.Ticks}";

            if (!_memCache.TryGetValue(cacheKey, out List<QuestionAnalyticsIndicatorDto> data))
            {
                await using var command = _commonService._unitOfWork.CreateDbCommand();
                command.CommandText = "GetQuestionAnalyticsIndicators";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@IndicatorPercentage", MySqlDbType.Decimal){ Value = filter.Percentage },
                    new MySqlParameter("@TypeOfIndicator", MySqlDbType.Int32){ Value = filter.Type },
                    new MySqlParameter("@FromDate", MySqlDbType.DateTime){ Value = filter.FromDate },
                    new MySqlParameter("@ToDate", MySqlDbType.DateTime){ Value = filter.ToDate }
                });

                await _commonService._unitOfWork.OpenConnectionAsync();
                data = [];

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    data.Add(new QuestionAnalyticsIndicatorDto(
                        reader.GetInt64("QuestionId"),
                        reader.IsDBNull("QuestionCode") ? null : reader.GetString("QuestionCode"),
                        reader.GetInt32("TotalAnswered"),
                        reader.GetInt32("CorrectCount"),
                        reader.GetInt32("WrongCount"),
                        reader.GetInt32("Percentage")
                    ));
                }

                _memCache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
            }

            return data;
        }

        private (bool IsValid, string Message) ValidateComprehensionAddedSubQuestion(QuestionMetadata? parentQuestionMetadataEntity, ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            if (parentQuestionMetadataEntity is null)
            {
                return (false, Resource.RootComprehensionQuestionNotfound);
            }

            if (string.IsNullOrWhiteSpace(comprehensionSubQuestionDto.SubQuestionMetadataDto.Code))
            {
                return (false, Resource.QuestionCodeRequired);
            }

            if (!parentQuestionMetadataEntity.IsRoot || parentQuestionMetadataEntity.ParentId != null)
            {
                return (false, Resource.ParentIdNotIndicateRootComprehensionQuestion);
            }

            if (parentQuestionMetadataEntity.QuestionTypeId == comprehensionSubQuestionDto.SubQuestionMetadataDto.QuestionTypeId)
            {
                return (false, Resource.SubQuestionCanNotBeComprehension);
            }

            if (!parentQuestionMetadataEntity.QuestionDetails.Select(d => d.LanguageId).Contains(comprehensionSubQuestionDto.SubQuestionDetailsDto.LanguageId))
            {
                return (false, Resource.SubQuestionLanguageMustBeInRootLanguages);
            }

            return (true, string.Empty);
        }

        private QuestionMetadata GetPreparedCopyOfRootComprehensionQuestionMetadata(QuestionMetadata parentQuestionMetadataEntity, ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            parentQuestionMetadataEntity.Id = 0;
            parentQuestionMetadataEntity.Code = comprehensionSubQuestionDto.SubQuestionMetadataDto.Code;
            parentQuestionMetadataEntity.IsRoot = false;
            parentQuestionMetadataEntity.QuestionTypeId = comprehensionSubQuestionDto.SubQuestionMetadataDto.QuestionTypeId;
            parentQuestionMetadataEntity.ParentId = comprehensionSubQuestionDto.SubQuestionMetadataDto.ParentId;
            parentQuestionMetadataEntity.Delta = comprehensionSubQuestionDto.SubQuestionMetadataDto.Delta;
            parentQuestionMetadataEntity.QuestionDetails = [];
            parentQuestionMetadataEntity.CreationUser = null;
            parentQuestionMetadataEntity.ModeficationUser = null;
            parentQuestionMetadataEntity.CreationDate = DateTimeHelper.Now;
            parentQuestionMetadataEntity.ModeficationDate = null;
            parentQuestionMetadataEntity.IsDeleted = false;
            parentQuestionMetadataEntity.IsActive = true;
            parentQuestionMetadataEntity.DeletedDate = null;

            return parentQuestionMetadataEntity;
        }

        private (bool IsValid, string Message) ValidateAddedSubQuestionLanguageVariant(QuestionMetadata? targetSubQuestionMetadataEntity, ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            if (targetSubQuestionMetadataEntity is null)
            {
                return (false, Resource.SubQuestionMetadataIdNotFound);
            }

            if (string.IsNullOrWhiteSpace(subQuestionLanguageVariantDto.SubQuestionMetadataDto.Code))
            {
                return (false, Resource.QuestionCodeRequired);
            }

            if (targetSubQuestionMetadataEntity.QuestionDetails.Select(d => d.LanguageId).Contains(subQuestionLanguageVariantDto.SubQuestionDetailsDto.LanguageId))
            {
                return (false, Resource.SubquestionsLanguageAlreadyExists);
            }

            return (true, string.Empty);
        }

        private (bool IsValid, string Message) ValidateUpdatedQuestionDetails(QuestionMetadata questionMetadataEntity, QuestionDetailsDto questionDetailsDto)
        {
            var questionDetailsEntity = questionMetadataEntity.QuestionDetails.FirstOrDefault(qd => qd.Id == questionDetailsDto.Id);

            if (questionDetailsEntity is null)
            {
                return (false, Resource.SpecifiedQuestionDetailsNotFound);
            }

            var otherQuestionDetailsOnSameMetadata = questionMetadataEntity.QuestionDetails.Where(qd => qd.Id != questionDetailsDto.Id);

            if (otherQuestionDetailsOnSameMetadata.Select(qd => qd.LanguageId).Contains(questionDetailsDto.LanguageId))
            {
                return (false, Resource.QuestionLanguageAlreadyAssigned);
            }

            return (true, string.Empty);
        }

        private async Task<(bool IsValid, string Message)> ValidateUpdatedSubQuestionLanguageVariantAsync(QuestionMetadata questionMetadataEntity, ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            var questionDetailsEntity = questionMetadataEntity.QuestionDetails.FirstOrDefault(qd => qd.Id == subQuestionLanguageVariantDto.SubQuestionDetailsDto.Id);

            if (questionDetailsEntity is null)
            {
                return (false, Resource.SubQuestionLanguageVariantDetailsNotFound);
            }

            if (string.IsNullOrWhiteSpace(subQuestionLanguageVariantDto.SubQuestionMetadataDto.Code))
            {
                return (false, Resource.QuestionCodeRequired);
            }

            var otherQuestionDetailsOnSameMetadata = questionMetadataEntity.QuestionDetails.Where(qd => qd.Id != subQuestionLanguageVariantDto.SubQuestionDetailsDto.Id);

            if (otherQuestionDetailsOnSameMetadata.Select(qd => qd.LanguageId).Contains(subQuestionLanguageVariantDto.SubQuestionDetailsDto.LanguageId))
            {
                return (false, Resource.QuestionLanguageAlreadyAssigned);
            }

            var rootComprehensionQuestionLanguagesGroup = (await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(qm => qm.Id == questionMetadataEntity.ParentId, Including: "QuestionDetails.Language"))
                .QuestionDetails
                .Select(qd => qd.LanguageId);

            if (!rootComprehensionQuestionLanguagesGroup.Contains(subQuestionLanguageVariantDto.SubQuestionDetailsDto.LanguageId))
            {
                return (false, Resource.SubQuestionLanguageMustBeInRootLanguages);
            }

            return (true, string.Empty);
        }

        private async Task<(bool IsValid, string Message, List<string> ProblematicQuestionsCodes, ValidateExcelSheetQuestionsDto ValidateExcelSheetQuestionsDto)> ValidateReceivedExcelSheetQuestionsAsync(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto)
        {
            // 1. Validate if there are any questions dtos provided in the request:

            var requestedQuestions = addMultipleQuestionsRequestDto.AddQuestionRequestDtos.ToList();

            if (requestedQuestions.Count == 0)
            {
                return (false, Resource.NoQuestionCodesProvided, null, null);
            }

            // 2. Fetch all questions from the database:

            var requestedCodes = requestedQuestions.ConvertAll(q => q.QuestionCode);

            var fetchedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q => requestedCodes.Contains(q.Code))
                .AsNoTracking()
                .Include(q => q.SubQuestions)
                .Include(q => q.QuestionDetails)
                .Include(q => q.QuestionType)
                .Select(q => new
                {
                    q.Id,
                    q.Code,
                    q.IsRoot,
                    q.QuestionStatus,
                    q.DifficultyProfileId,
                    q.QuestionType,
                    q.QuestionDetails,
                    SubQuestionsCount = q.QuestionType.Name == nameof(Helper.Enums.QuestionType.Comprehension) ? (q.SubQuestions.Count < 1 ? 1 : q.SubQuestions.Count) : 1
                })
                .ToListAsync();

            if (fetchedQuestions.Count == 0)
            {
                return (false, Resource.Nomatchingquestionsfoundforprovidedcodes, requestedCodes, null);
            }

            // 3. Validate if there are any missing codes:

            var foundCodes = fetchedQuestions.Select(q => q.Code).ToHashSet();

            var missingCodes = requestedCodes.Where(code => !foundCodes.Contains(code)).ToList();

            if (missingCodes.Count > 0)
            {
                return (false, $"{Resource.Questioncodeswerenotfound}: {string.Join(", ", missingCodes)}", missingCodes, null);
            }

            // 4. Validate if all requested questions are approved:

            if (!fetchedQuestions.TrueForAll(q => q.QuestionStatus == QuestionStatus.Approved))
            {
                return (false, Resource.AllProvidedQuestionsShouldHaveStatusApproved, null, null);
            }

            // 5. Validate if there are any non-auto correctable questions, in case of instant result is allowed:

            if (addMultipleQuestionsRequestDto.PaperAllowInstantResult)
            {
                var nonAutoCorrectableQuestionsCodes = fetchedQuestions
                    .Where(q => !q.QuestionType.IsAutoCorrectable)
                    .Select(q => q.Code)
                    .ToList();

                if (nonAutoCorrectableQuestionsCodes.Count > 0)
                {
                    return (false, $"{Resource.Thefollowingquestionsarenotautocorrectable}: {string.Join(", ", nonAutoCorrectableQuestionsCodes)}", nonAutoCorrectableQuestionsCodes, null);
                }
            }

            // 6. Validate if all questions are root questions:

            if (!fetchedQuestions.TrueForAll(q => q.IsRoot))
            {
                return (false, Resource.AllProvidedQuestionsShouldBeRoot, null, null);
            }

            // 7. Validate if all questions have the same difficulty profile:

            var targetDifficultyLevelId = fetchedQuestions.FirstOrDefault().DifficultyProfileId;
            if (!fetchedQuestions.TrueForAll(q => q.DifficultyProfileId == targetDifficultyLevelId))
            {
                return (false, Resource.AllProvidedQuestionsShouldBeOnSameDifficultyProfile, null, null);
            }

            // 8. Validate if all questions has at least the same language variant:

            var commonLanguages = fetchedQuestions
                .Select(q => q.QuestionDetails.Select(d => d.LanguageId).ToHashSet())
                .Aggregate((current, next) => { current.IntersectWith(next); return current; });

            if (commonLanguages.Count == 0)
            {
                return (false, Resource.AllProvidedQuestionsShouldHaveAtLeastOneCommonLanguage, null, null);
            }

            // 9. Return successful response, if all validations succeeded:

            var validQuestions = requestedQuestions.Join(
                fetchedQuestions,
                req => req.QuestionCode,
                db => db.Code,
                (req, db) => new QuestionWithSectionNameDto(
                    db.Id,
                    req.SectionName,
                    db.SubQuestionsCount
                )
            ).ToList();

            var resultedDto = new ValidateExcelSheetQuestionsDto(
                commonLanguages,
                targetDifficultyLevelId,
                validQuestions
            );

            return (true, Resource.AllQuestionsAreValid, null, resultedDto);
        }

        private static string ConvertDigitsToArabicInText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return html;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var textNode in doc.DocumentNode.DescendantsAndSelf().Where(n => n.NodeType == HtmlNodeType.Text))
            {
                if (string.IsNullOrWhiteSpace(textNode.InnerText))
                {
                    continue;
                }

                textNode.InnerHtml = ConvertDigits(textNode.InnerText);
            }

            return doc.DocumentNode.OuterHtml;
        }

        private static string ConvertDigits(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            if (Regex.IsMatch(text.TrimStart(), RegularExpressions.JsonStartPattern))
            {
                return Regex.Replace(text, RegularExpressions.JsonStringValue, m =>
                {
                    if (Regex.IsMatch(m.Value, RegularExpressions.IsUrlOrEmail))
                    {
                        return m.Value;
                    }

                    return Regex.Replace(m.Value, RegularExpressions.NonUnicodeDigit,
                           n => ((char)('٠' + (n.Value[0] - '0'))).ToString());
                });
            }

            return Regex.Replace(text, RegularExpressions.NonUrlNonUnicodeDigit,
                   m => ((char)('٠' + (m.Value[0] - '0'))).ToString());
        }

        public async Task<bool> IsQuestionLinkedToPaperAsync(long questionMetadataId)
        {
            // Check if question is used in any form
            var isInForm = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAll(q => q.QuestionId == questionMetadataId && !q.Form.IsDeleted)
                .AsNoTracking()
                .AnyAsync();

            // Check if question is used in any block
            var isInBlock = await _commonService
                ._unitOfWork
                .Repository<BlockQuestion, long>()
                .GetAll(q => q.QuestionMetadataId == questionMetadataId && !q.Block.IsDeleted)
                .AsNoTracking()
                .AnyAsync();

            return isInForm || isInBlock;
        }

        private async Task HandleOrderingQuestionModelAnswer(QuestionDetails questionDetails, long questionTypeId)
        {
            if (questionTypeId == (long)Helper.Enums.QuestionType.Ordering)
            {
                var answers = questionDetails.QuestionsChoices.OrderBy(c => c.OrderId).Select(c => new OrderingQuestionAnswerDto
                {
                    ChoiceId = c.Id,
                    OrderId = c.OrderId
                }).ToList();

                questionDetails.ModelAnswer = JsonSerializer.Serialize(answers);

                await _commonService._unitOfWork.Complete();
            }
        }

        #region Segment Question
        private async Task<ApiResponse> CreateSegmentSubQuestionMetadataCopiesAsync(QuestionMetadata parentQuestion, int copiesCount)
        {
            if (copiesCount < 1)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    string.Empty,
                    new List<QuestionMetadata>()
                );
            }

            var copies = Enumerable
                .Range(0, copiesCount)
                .Select(_ => CreateSegmentQuestionMetadataCopy(parentQuestion))
                .ToList();

            _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .AddRangAsync(copies);

            var saved = await _commonService._unitOfWork.Complete();

            if (saved > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    string.Empty,
                    copies
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError
            );
        }

        private static QuestionMetadata CreateSegmentQuestionMetadataCopy(QuestionMetadata parent) => new()
        {
            IsRoot = false,
            ParentId = parent.Id,
            CurrentExhaustionCount = 0,
            IsActive = true,
            Delta = parent.Delta,
            MaximumAnswerTime = parent.MaximumAnswerTime,
            Author = parent.Author,
            QuestionStatus = parent.QuestionStatus,
            ScientificEditorPanelEnabled = parent.ScientificEditorPanelEnabled,
            FileManagerEditorPanelEnabled = parent.FileManagerEditorPanelEnabled,
            QuestionTypeId = parent.QuestionTypeId,
            QuestionCategoryId = parent.QuestionCategoryId,
            QuestionsExhaustionCount = parent.QuestionsExhaustionCount,
            SubjectId = parent.SubjectId,
            IloId = parent.IloId,
            ItemBankId = parent.ItemBankId,
            DifficultyProfileId = parent.DifficultyProfileId,
            DifficultyLevelId = parent.DifficultyLevelId,
            QuestionLayoutId = parent.QuestionLayoutId
        };

        private (List<QuestionDetails> ToInsert, List<QuestionDetails> ToUpdate) AddSegmentQuestionDetails(
            List<SegmentQuestionDto> dtos,
            List<QuestionMetadata> metadataList,
            List<QuestionDetails> existingDetails
        )
        {
            var toInsert = new List<QuestionDetails>();
            var toUpdate = new List<QuestionDetails>();

            var existingByMetadataId = existingDetails.ToDictionary(x => x.QuestionMetadataId);

            for (int i = 0; i < dtos.Count && i < metadataList.Count; i++)
            {
                var dto = dtos[i];
                var metadataId = metadataList[i].Id;

                metadataList[i].Code = dto.SegmentMetaDataDto.Code;

                if (existingByMetadataId.TryGetValue(metadataId, out var entity))
                {
                    MapSegmentDetails(entity, dto);
                    toUpdate.Add(entity);
                }
                else
                {
                    var newEntity = CreateSegmentDetails(dto, metadataId);
                    toInsert.Add(newEntity);
                }
            }

            return (toInsert, toUpdate);
        }

        private QuestionDetails CreateSegmentDetails(SegmentQuestionDto dto, long metadataId)
        {
            var details = dto.SegmentQuestionDetailsDto;

            var entity = new QuestionDetails
            {
                Body = details.Body,
                Instructions = details.Instructions,
                ModelAnswer = details.ModelAnswer,
                LanguageId = details.LanguageId,
                AttachmentFileName = details.MediaFileName,
                QuestionMetadataId = metadataId,
                MaxRecordingTimeInSeconds = details.MaxRecordingTimeInSeconds
            };

            dto.SegmentQuestionDetailsDto.QuestionMetadataId = metadataId;

            if (dto.SegmentQuestionConfigDto != null)
            {
                entity.SegmentQuestionProperties = _mapper.Map<SegmentQuestionProperties>(dto.SegmentQuestionConfigDto);
            }

            return entity;
        }

        private void MapSegmentDetails(QuestionDetails entity, SegmentQuestionDto dto)
        {
            var details = dto.SegmentQuestionDetailsDto;

            entity.Body = details.Body;
            entity.Instructions = details.Instructions;
            entity.ModelAnswer = details.ModelAnswer;
            entity.LanguageId = details.LanguageId;
            entity.AttachmentFileName = details.MediaFileName;
            entity.MaxWords = details.MaxWords;
            entity.MaxRecordingTimeInSeconds = details.MaxRecordingTimeInSeconds;

            if (dto.SegmentQuestionConfigDto == null)
                return;

            entity.SegmentQuestionProperties ??= new SegmentQuestionProperties();

            _mapper.Map(dto.SegmentQuestionConfigDto, entity.SegmentQuestionProperties);
        }

        private async Task SaveSegmentQuestionMediaAsync(List<SegmentQuestionDto> segmentQuestionDtos)
        {
            List<QuestionDocLibFile> mediaEntities = [];

            foreach (var segmentDto in segmentQuestionDtos)
            {
                var extractedFiles = new List<FileUrlWithFileId>();
                var questionMetaDataId = segmentDto.SegmentQuestionDetailsDto.QuestionMetadataId;
                var details = segmentDto.SegmentQuestionDetailsDto;
                var configs = segmentDto.SegmentQuestionConfigDto;

                if (details == null) continue;

                if (!string.IsNullOrWhiteSpace(details.Body))
                {
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(details.Body));
                }

                if (!string.IsNullOrWhiteSpace(details.ModelAnswer))
                {
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(details.ModelAnswer));
                }

                if (!string.IsNullOrWhiteSpace(details.Instructions))
                {
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(details.Instructions));
                }

                if (!string.IsNullOrWhiteSpace(configs.SegmentAudioUrl))
                    extractedFiles.AddRange(_htmlHelperService.ExtractDocumentUrlsAndIds(configs.SegmentAudioUrl));

                var existingMediaFiles = await _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .GetAll(qf => qf.QuestionId == questionMetaDataId)
                    .ToListAsync();

                if (existingMediaFiles.Count > 0)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<QuestionDocLibFile, long>()
                        .DeleteRange(existingMediaFiles);
                }

                if (extractedFiles.Count > 0)
                {
                    mediaEntities.AddRange(
                        [.. extractedFiles
                            .DistinctBy(x => x.FileId)
                            .Select(file => new QuestionDocLibFile
                            {
                                QuestionId = questionMetaDataId,
                                FileURL = file.Url,
                                FileId = Guid.Parse(file.FileId)
                            })]
                    );
                }
            }

            if (mediaEntities.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionDocLibFile, long>()
                    .AddRangAsync(mediaEntities);
            }

            await _commonService._unitOfWork.Complete();
        }

        private static SegmentQuestionDto MapToSegmentQuestionDto(QuestionMetadata questionMetadata, QuestionDetails questionDetails)
        {
            var segmentQuestionDto = new SegmentQuestionDto
            {
                SegmentMetaDataDto = new SegmentQuestionMetaDataDto
                {
                    ParentId = (questionMetadata.ParentId ?? 0),
                    Code = questionMetadata.Code
                },
                SegmentQuestionDetailsDto = new SegmentQuestionDetailsDto
                {
                    Id = questionDetails.Id,
                    Body = questionDetails.Body,
                    Instructions = questionDetails.Instructions,
                    ModelAnswer = questionDetails.ModelAnswer,
                    LanguageId = questionDetails.LanguageId,
                    MediaFileName = questionDetails.AttachmentFileName,
                    QuestionMetadataId = questionDetails.QuestionMetadataId,
                    MaxRecordingTimeInSeconds = questionDetails.MaxRecordingTimeInSeconds,
                    Choices = [.. questionDetails.QuestionsChoices
                        .Select(qc => new ChoiceDataDto
                        {
                            Id = qc.Id,
                            ChoiceText = qc.ChoiceText,
                            IsCorrectAnswer = qc.IsCorrectAnswer,
                            AttachmentFileName = qc.AttachmentFileName
                        })
                    ]
                }
            };

            if (questionDetails.SegmentQuestionProperties is not null)
            {
                segmentQuestionDto.SegmentQuestionConfigDto = new SegmentQuestionConfigDto
                {
                    ThinkingTime = questionDetails.SegmentQuestionProperties.ThinkingTime,
                    ResponseTime = questionDetails.SegmentQuestionProperties.ResponseTime,
                    WordsCount = questionDetails.SegmentQuestionProperties.WordsCount,
                    OrderNumber = questionDetails.SegmentQuestionProperties.OrderNumber,
                    HasScore = questionDetails.SegmentQuestionProperties.HasScore,
                    SegmentQuestionResponseType = questionDetails.SegmentQuestionProperties.SegmentQuestionResponseType,
                    SegmentAudioUrl = questionDetails.SegmentQuestionProperties.SegmentAudioUrl ?? string.Empty
                };
            }

            return segmentQuestionDto;
        }
        #endregion Segment Question

        #region Matching Pairs & Matching Pairs With Drag And Drop
        private async Task<(bool IsValid, string Message)> ValidateMatchingPairsAsync(AddOrUpdateMatchingPairsRequestDto request)
        {
            return await ValidateMatchingPairsCommonAsync(request.MetadataParentId, request.QuestionDetails, request.MatchingItems);
        }

        private async Task<(bool IsValid, string Message)> ValidateMatchingPairsWithDragDropAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto request)
        {
            return await ValidateMatchingPairsCommonAsync(request.MetadataParentId, request.QuestionDetails, request.MatchingItems);
        }

        private async Task<(bool IsValid, string Message)> ValidateMatchingPairsCommonAsync(
            long metadataParentId,
            List<QuestionDetailsDto> questionDetails,
            List<MatchingPairQuestionItemDto> matchingItems)
        {
            if (matchingItems == null)
            {
                return (false, Resource.MatchingAnswerCannotBeEmpty);
            }

            if (matchingItems.Count < 4)
            {
                return (false, Resource.MatchingPairsMinimumFour);
            }

            var firstDetail = questionDetails.First();

            bool languageConflict = await _commonService
                    ._unitOfWork
                    .Repository<QuestionDetails, long>()
                    .IsExistAsync(x =>
                        x.QuestionMetadataId == metadataParentId &&
                        x.LanguageId == firstDetail.LanguageId &&
                        x.Id != firstDetail.Id
                    );

            if (languageConflict)
            {
                return (false, Resource.QuestionLanguageAlreadyAssigned);
            }

            return (true, string.Empty);
        }

        private async Task DeleteExistingMatchingPairsWithDragDropAsync(long parentMetadataId, long questionDetailsId)
        {
            await _commonService
                ._unitOfWork
                .Repository<MatchingPairQuestionItems, long>()
                .Query()
                .Where(m => m.QuestionDetailsId == questionDetailsId)
                .ExecuteDeleteAsync();

            await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .Where(d => d.Id == questionDetailsId && d.QuestionMetadataId == parentMetadataId)
                .ExecuteDeleteAsync();
        }
        #endregion Matching Pairs & Matching Pairs With Drag And Drop

        private static bool HasQuestionPropertiesChanged(QuestionDetails current, QuestionDetailsDto dto, QuestionTypeEnum questionType)
        {
            var dtoBody = dto.UseArabicNumbers ? ConvertDigitsToArabicInText(dto.Body) : dto.Body;
            var dtoInstructions = dto.UseArabicNumbers ? ConvertDigitsToArabicInText(dto.Instructions) : dto.Instructions;
            var dtoModelAnswer = dto.UseArabicNumbers
                ? ConvertModelAnswerDigitsForQuestionType(dto.ModelAnswer, questionType)
                : dto.ModelAnswer;

            return current.Body != dtoBody ||
                   current.Instructions != dtoInstructions ||
                   current.ModelAnswer != dtoModelAnswer ||
                   current.LanguageId != dto.LanguageId ||
                   current.HasShuffled != dto.HasShuffled ||
                   current.MaxWords != dto.MaxWords ||
                   current.MaxRecordingTimeInSeconds != dto.MaxRecordingTimeInSeconds ||
                   current.UseArabicNumbers != dto.UseArabicNumbers;
        }

        private static bool HasChoicesChanged(QuestionDetails current, QuestionDetailsDto dto)
        {
            var currentChoices = current.QuestionsChoices ?? [];
            var dtoChoices = dto.Choices ?? [];

            if (currentChoices.Count != dtoChoices.Count)
            {
                return true;
            }

            var dtoChoicesForComparison = dtoChoices.Select(c => new
            {
                ChoiceText = dto.UseArabicNumbers ? ConvertDigitsToArabicInText(c.ChoiceText) : c.ChoiceText,
                c.IsCorrectAnswer,
                c.AttachmentFileName
            }).OrderBy(c => c.ChoiceText).ToList();

            var currentChoicesForComparison = currentChoices
                .Select(c => new
                {
                    c.ChoiceText,
                    c.IsCorrectAnswer,
                    c.AttachmentFileName
                })
                .OrderBy(c => c.ChoiceText)
                .ToList();

            for (int i = 0; i < dtoChoicesForComparison.Count; i++)
            {
                if (dtoChoicesForComparison[i].ChoiceText != currentChoicesForComparison[i].ChoiceText ||
                    dtoChoicesForComparison[i].IsCorrectAnswer != currentChoicesForComparison[i].IsCorrectAnswer ||
                    dtoChoicesForComparison[i].AttachmentFileName != currentChoicesForComparison[i].AttachmentFileName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasQuestionDetailsChanged(QuestionDetails currentEntity, QuestionDetailsDto dtoChanges, QuestionTypeEnum questionType)
        {
            if (HasQuestionPropertiesChanged(currentEntity, dtoChanges, questionType))
            {
                return true;
            }

            if (HasChoicesChanged(currentEntity, dtoChanges))
            {
                return true;
            }

            return false;
        }

        private async Task DeleteAllQuestionVersionsForMetadataTreeAsync(long rootMetadataId, long? languageId = null)
        {
            var allMetadataIds = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(m => m.Id == rootMetadataId || m.ParentId == rootMetadataId)
                .Select(m => m.Id)
                .ToListAsync();

            var versionQuery = _commonService
                ._unitOfWork
                .Repository<QuestionDetailsVersions, long>()
                .GetAll(v => allMetadataIds.Contains(v.QuestionMetadataId));

            if (languageId.HasValue)
            {
                versionQuery = versionQuery.Where(v => v.LanguageId == languageId.Value);
            }

            var questionDetailsVersions = await versionQuery.ToListAsync();

            if (questionDetailsVersions.Count == 0)
            {
                return;
            }

            var allQuestionDetailsIds = questionDetailsVersions
                .Select(v => v.QuestionDetailsId)
                .Distinct()
                .ToList();

            var versionKeys = questionDetailsVersions
                .Select(v => (QuestionDetailsId: v.QuestionDetailsId, VersionNumber: (long)v.VersionNumber))
                .ToHashSet();

            foreach (var version in questionDetailsVersions)
            {
                version.IsDeleted = true;
                version.IsActive = false;
                version.DeletedDate = DateTimeHelper.Now;
            }

            var segmentPropertiesVersionIds = questionDetailsVersions
                .Where(v => v.SegmentQuestionPropertiesVersionsId.HasValue)
                .Select(v => v.SegmentQuestionPropertiesVersionsId!.Value)
                .Distinct()
                .ToList();

            if (segmentPropertiesVersionIds.Count > 0)
            {
                var segmentPropertiesVersions = await _commonService
                    ._unitOfWork
                    .Repository<SegmentQuestionPropertiesVersions, long>()
                    .GetAll(v => segmentPropertiesVersionIds.Contains(v.Id))
                    .ToListAsync();

                if (segmentPropertiesVersions.Count > 0)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<SegmentQuestionPropertiesVersions, long>()
                        .DeleteRange(segmentPropertiesVersions);
                }
            }

            var choicesVersions = await _commonService
                ._unitOfWork
                .Repository<QuestionsChoicesVersions, long>()
                .GetAll(v => allQuestionDetailsIds.Contains(v.QuestionDetailsId))
                .ToListAsync();

            if (languageId.HasValue)
            {
                choicesVersions = [.. choicesVersions.Where(c => versionKeys.Contains((c.QuestionDetailsId, c.VersionNumber)))];
            }

            foreach (var choiceVersion in choicesVersions)
            {
                choiceVersion.IsDeleted = true;
                choiceVersion.IsActive = false;
                choiceVersion.DeletedDate = DateTimeHelper.Now;
            }

            var matchingPairItemsVersions = await _commonService
                ._unitOfWork
                .Repository<MatchingPairQuestionItemsVersions, long>()
                .GetAll(v => allQuestionDetailsIds.Contains(v.QuestionDetailsId))
                .ToListAsync();

            if (languageId.HasValue)
            {
                matchingPairItemsVersions = [.. matchingPairItemsVersions.Where(m => versionKeys.Contains((m.QuestionDetailsId, m.VersionNumber)))];
            }

            if (matchingPairItemsVersions.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<MatchingPairQuestionItemsVersions, long>()
                    .DeleteRange(matchingPairItemsVersions);
            }
        }

        private async Task<int> CreateQuestionVersion(QuestionDetails questionDetailsEntity, int? explicitVersion = null)
        {
            var maxVersion = await _commonService
                ._unitOfWork
                .Repository<QuestionDetailsVersions, long>()
                .Query()
                .Where(x => x.QuestionMetadataId == questionDetailsEntity.QuestionMetadataId)
                .MaxAsync(x => (int?)x.VersionNumber) ?? 0;

            SegmentQuestionPropertiesVersions? segmentQuestionPropertiesVersion = null;

            if (questionDetailsEntity.SegmentQuestionProperties != null)
            {
                segmentQuestionPropertiesVersion = new SegmentQuestionPropertiesVersions
                {
                    HasScore = questionDetailsEntity.SegmentQuestionProperties.HasScore,
                    ThinkingTime = questionDetailsEntity.SegmentQuestionProperties.ThinkingTime,
                    ResponseTime = questionDetailsEntity.SegmentQuestionProperties.ResponseTime,
                    WordsCount = questionDetailsEntity.SegmentQuestionProperties.WordsCount,
                    OrderNumber = questionDetailsEntity.SegmentQuestionProperties.OrderNumber,
                    SegmentAudioUrl = questionDetailsEntity.SegmentQuestionProperties.SegmentAudioUrl,
                    SegmentQuestionResponseType = questionDetailsEntity.SegmentQuestionProperties.SegmentQuestionResponseType
                };

                await _commonService._unitOfWork.Repository<SegmentQuestionPropertiesVersions, long>().AddAsync(segmentQuestionPropertiesVersion);
            }

            var questionDetailsVersion = new QuestionDetailsVersions
            {
                QuestionDetailsId = questionDetailsEntity.Id,
                Body = questionDetailsEntity.Body,
                Instructions = questionDetailsEntity.Instructions,
                ModelAnswer = questionDetailsEntity.ModelAnswer,
                QuestionMetadataId = questionDetailsEntity.QuestionMetadataId,
                LanguageId = questionDetailsEntity.LanguageId,
                HasShuffled = questionDetailsEntity.HasShuffled,
                MaxWords = questionDetailsEntity.MaxWords,
                MaxRecordingTimeInSeconds = questionDetailsEntity.MaxRecordingTimeInSeconds,
                UseArabicNumbers = questionDetailsEntity.UseArabicNumbers,
                AttachmentFileName = questionDetailsEntity.AttachmentFileName,
                VersionNumber = explicitVersion ?? (maxVersion + 1),
                CreationDate = DateTimeHelper.Now,
                CreationUser = questionDetailsEntity.CreationUser,
                SegmentQuestionPropertiesVersions = segmentQuestionPropertiesVersion
            };

            await _commonService._unitOfWork.Repository<QuestionDetailsVersions, long>().AddAsync(questionDetailsVersion);

            if (questionDetailsEntity.QuestionsChoices?.Count > 0)
            {
                foreach (var choice in questionDetailsEntity.QuestionsChoices)
                {
                    var questionChoiceVersion = new QuestionsChoicesVersions
                    {
                        VersionNumber = questionDetailsVersion.VersionNumber,
                        QuestionDetailsId = questionDetailsEntity.Id,
                        ChoiceText = choice.ChoiceText,
                        IsCorrectAnswer = choice.IsCorrectAnswer,
                        AttachmentFileName = choice.AttachmentFileName,
                        CreationDate = DateTimeHelper.Now,
                        CreationUser = choice.CreationUser
                    };

                    await _commonService._unitOfWork.Repository<QuestionsChoicesVersions, long>().AddAsync(questionChoiceVersion);
                }
            }

            if (questionDetailsEntity.MatchingPairQuestionItems?.Count > 0)
            {
                foreach (var item in questionDetailsEntity.MatchingPairQuestionItems)
                {
                    var itemVersion = new MatchingPairQuestionItemsVersions
                    {
                        VersionNumber = questionDetailsVersion.VersionNumber,
                        QuestionDetailsId = questionDetailsEntity.Id,
                        Body = item.Body,
                        ColumnOrder = item.ColumnOrder,
                        IsDataSource = item.IsDataSource
                    };

                    await _commonService._unitOfWork.Repository<MatchingPairQuestionItemsVersions, long>().AddAsync(itemVersion);
                }
            }

            return questionDetailsVersion.VersionNumber;
        }

        private async Task CreateComprehensionQuestionVersions(long rootQuestionMetadataId, long languageId)
        {
            var rootQuestion = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == rootQuestionMetadataId)
                .Include(qm => qm.QuestionDetails)
                .ThenInclude(qd => qd.QuestionsChoices)
                .FirstOrDefaultAsync();

            if (rootQuestion == null) return;

            var mainQuestionDetails = rootQuestion
                .QuestionDetails
                .FirstOrDefault(qd => qd.LanguageId == languageId);

            int? nextVersion = null;
            if (mainQuestionDetails != null)
            {
                nextVersion = await CreateQuestionVersion(mainQuestionDetails);
            }

            var subQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.ParentId == rootQuestionMetadataId)
                .Include(qm => qm.QuestionDetails)
                .ThenInclude(qd => qd.QuestionsChoices)
                .ToListAsync();

            foreach (var subQuestion in subQuestions)
            {
                var subQuestionDetails = subQuestion
                    .QuestionDetails
                    .FirstOrDefault(qd => qd.LanguageId == languageId);

                if (subQuestionDetails != null)
                {
                    await CreateQuestionVersion(subQuestionDetails, nextVersion);
                }
            }
        }

        private async Task CreateSegmentQuestionVersionsAsync(long parentMetadataId, long languageId)
        {
            var parentQuestion = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.Id == parentMetadataId)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.QuestionsChoices)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.SegmentQuestionProperties)
                .FirstOrDefaultAsync();

            if (parentQuestion == null) return;

            var parentDetails = parentQuestion.QuestionDetails
                .FirstOrDefault(qd => qd.LanguageId == languageId);

            int? nextVersion = null;
            if (parentDetails != null)
                nextVersion = await CreateQuestionVersion(parentDetails);

            var subQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(qm => qm.ParentId == parentMetadataId)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.QuestionsChoices)
                .Include(qm => qm.QuestionDetails)
                    .ThenInclude(qd => qd.SegmentQuestionProperties)
                .ToListAsync();

            foreach (var subQuestion in subQuestions)
            {
                var subDetails = subQuestion.QuestionDetails
                    .FirstOrDefault(qd => qd.LanguageId == languageId);

                if (subDetails != null)
                    await CreateQuestionVersion(subDetails, nextVersion);
            }
        }

        private static bool HasSubQuestionPropertiesChanged(QuestionDetails current, SubQuestionDetailsDto dto)
        {
            return current.Body != dto.Body ||
                   current.Instructions != dto.Instructions ||
                   current.ModelAnswer != dto.ModelAnswer ||
                   current.QuestionMetadataId != dto.QuestionMetadataId ||
                   current.LanguageId != dto.LanguageId ||
                   current.MaxWords != dto.MaxWords ||
                   current.MaxRecordingTimeInSeconds != dto.MaxRecordingTimeInSeconds;
        }

        private static bool HasSubQuestionChoicesChanged(QuestionDetails current, SubQuestionDetailsDto dto)
        {
            var currentChoices = current.QuestionsChoices ?? [];
            var dtoChoices = dto.Choices ?? [];

            if (currentChoices.Count != dtoChoices.Count)
            {
                return true;
            }

            var dtoChoicesForComparison = dtoChoices.Select(c => new
            {
                c.ChoiceText,
                c.IsCorrectAnswer,
                c.AttachmentFileName
            }).OrderBy(c => c.ChoiceText).ToList();

            var currentChoicesForComparison = currentChoices
                .Select(c => new
                {
                    c.ChoiceText,
                    c.IsCorrectAnswer,
                    c.AttachmentFileName
                })
                .OrderBy(c => c.ChoiceText)
                .ToList();

            for (int i = 0; i < dtoChoicesForComparison.Count; i++)
            {
                if (dtoChoicesForComparison[i].ChoiceText != currentChoicesForComparison[i].ChoiceText ||
                    dtoChoicesForComparison[i].IsCorrectAnswer != currentChoicesForComparison[i].IsCorrectAnswer ||
                    dtoChoicesForComparison[i].AttachmentFileName != currentChoicesForComparison[i].AttachmentFileName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSubQuestionDetailsChanged(QuestionDetails currentEntity, SubQuestionDetailsDto dtoChanges)
        {
            if (HasSubQuestionPropertiesChanged(currentEntity, dtoChanges))
            {
                return true;
            }

            if (HasSubQuestionChoicesChanged(currentEntity, dtoChanges))
            {
                return true;
            }

            return false;
        }

        private static bool HasSegmentQuestionDetailsChanged(QuestionDetails current, SegmentQuestionDto dto)
        {
            var details = dto.SegmentQuestionDetailsDto;
            var config = dto.SegmentQuestionConfigDto;

            bool propertiesChanged =
                current.Body != details.Body ||
                current.Instructions != details.Instructions ||
                current.ModelAnswer != details.ModelAnswer ||
                current.LanguageId != details.LanguageId ||
                current.AttachmentFileName != details.MediaFileName ||
                current.MaxRecordingTimeInSeconds != details.MaxRecordingTimeInSeconds ||
                current.MaxWords != details.MaxWords;

            if (propertiesChanged) return true;

            if (config == null && current.SegmentQuestionProperties == null) return false;
            if (config == null || current.SegmentQuestionProperties == null) return true;

            return current.SegmentQuestionProperties.OrderNumber != config.OrderNumber ||
                   current.SegmentQuestionProperties.WordsCount != config.WordsCount ||
                   current.SegmentQuestionProperties.HasScore != config.HasScore ||
                   current.SegmentQuestionProperties.ResponseTime != config.ResponseTime ||
                   current.SegmentQuestionProperties.ThinkingTime != config.ThinkingTime ||
                   current.SegmentQuestionProperties.SegmentQuestionResponseType != config.SegmentQuestionResponseType ||
                   current.SegmentQuestionProperties.SegmentAudioUrl != config.SegmentAudioUrl;
        }

        private static bool HasMatchingPairsChanged(QuestionDetails existingDetails, AddOrUpdateMatchingPairsRequestDto request)
            => HasMatchingPairsItemsChanged(existingDetails, request.QuestionDetails, request.MatchingItems);

        private static bool HasMatchingPairsWithDragDropChanged(QuestionDetails existingDetails, AddOrUpdateMatchingPairsWithDragDropRequestDto request)
            => HasMatchingPairsItemsChanged(existingDetails, request.QuestionDetails, request.MatchingItems);

        private static bool HasMatchingPairsItemsChanged(
            QuestionDetails existingDetails,
            List<QuestionDetailsDto> questionDetails,
            List<MatchingPairQuestionItemDto> matchingItems)
        {
            var firstDto = questionDetails[0];

            var dtoBody = firstDto.UseArabicNumbers
                ? ConvertDigitsToArabicInText(firstDto.Body)
                : firstDto.Body;

            var dtoInstructions = firstDto.UseArabicNumbers
                ? ConvertDigitsToArabicInText(firstDto.Instructions)
                : firstDto.Instructions;

            if (existingDetails.Body != dtoBody ||
                existingDetails.Instructions != dtoInstructions ||
                existingDetails.UseArabicNumbers != firstDto.UseArabicNumbers ||
                existingDetails.HasShuffled != firstDto.HasShuffled ||
                existingDetails.MaxWords != firstDto.MaxWords ||
                existingDetails.MaxRecordingTimeInSeconds != firstDto.MaxRecordingTimeInSeconds)
            {
                return true;
            }

            var existingItems = (existingDetails.MatchingPairQuestionItems ?? [])
                .OrderBy(x => x.ColumnOrder)
                .ThenBy(x => x.Body)
                .ToList();

            var incomingItems = (matchingItems ?? [])
                .OrderBy(x => x.ColumnOrder)
                .ThenBy(x => x.Body)
                .ToList();

            if (existingItems.Count != incomingItems.Count)
                return true;

            for (int i = 0; i < existingItems.Count; i++)
            {
                if (existingItems[i].Body != incomingItems[i].Body ||
                    existingItems[i].ColumnOrder != incomingItems[i].ColumnOrder ||
                    existingItems[i].IsDataSource != incomingItems[i].IsDataSource)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsQuestionMetadataUsed(QuestionMetadata? metadata) =>
           (metadata?.FormQuestions?.Count > 0) ||
           (metadata?.BlocksQuestions?.Count > 0) ||
           (metadata?.ManualPaperItemBankQuestionSections?.Count > 0) ||
           (metadata?.ParentSubQuestion?.FormQuestions?.Count > 0) ||
           (metadata?.ParentSubQuestion?.BlocksQuestions?.Count > 0) ||
           (metadata?.ParentSubQuestion?.ManualPaperItemBankQuestionSections?.Count > 0);

        private async Task<bool> GetItemBankUnscoredAsync(long itemBankId)
        {
            return await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .Where(x => x.Id == itemBankId)
                .Select(x => x.Unscored)
                .FirstOrDefaultAsync();
        }

        private static string ConvertModelAnswerDigitsForQuestionType(string modelAnswer, QuestionTypeEnum questionType)
        {
            if (string.IsNullOrWhiteSpace(modelAnswer))
            {
                return modelAnswer;
            }

            switch (questionType)
            {
                case QuestionTypeEnum.HotSpotWithDragDrop:
                case QuestionTypeEnum.HotSpotWithClicks:
                    {
                        var dto = JsonSerializer.Deserialize<HotSpotQuestionDetailsDto>(modelAnswer, _jsonSerializerOptions);

                        if (dto == null)
                        {
                            return modelAnswer;
                        }

                        foreach (var item in dto.DraggableItems)
                        {
                            item.Label = ConvertDigitsToArabicInText(item.Label);
                        }

                        foreach (var area in dto.HotSpotAreas)
                        {
                            area.Feedback = ConvertDigitsToArabicInText(area.Feedback);
                        }

                        return JsonSerializer.Serialize(dto, _jsonSerializerOptions);
                    }

                case QuestionTypeEnum.WebSearch:
                    {
                        var dto = JsonSerializer.Deserialize<WebSearchPropertiesDto>(modelAnswer, _jsonSerializerOptions);

                        if (dto == null)
                        {
                            return modelAnswer;
                        }

                        dto.Keywords = [.. dto.Keywords.Select(ConvertDigitsToArabicInText)];

                        foreach (var result in dto.Results)
                        {
                            result.Title = ConvertDigitsToArabicInText(result.Title);
                            result.Description = ConvertDigitsToArabicInText(result.Description);
                        }

                        return JsonSerializer.Serialize(dto, _jsonSerializerOptions);
                    }

                case QuestionTypeEnum.WebRegistration:
                    {
                        var dto = JsonSerializer.Deserialize<WebRegistrationExpectedDataDto>(modelAnswer, _jsonSerializerOptions);

                        if (dto == null)
                        {
                            return modelAnswer;
                        }

                        dto.ExpectedFirstName = ConvertDigitsToArabicInText(dto.ExpectedFirstName);

                        dto.ExpectedLastName = ConvertDigitsToArabicInText(dto.ExpectedLastName);

                        return JsonSerializer.Serialize(dto, _jsonSerializerOptions);
                    }

                case QuestionTypeEnum.FillInTheBlank:
                    {
                        var dto = JsonSerializer.Deserialize<List<FillBlankAnswerDto>>(modelAnswer, _jsonSerializerOptions);

                        if (dto == null)
                        {
                            return modelAnswer;
                        }

                        foreach (var item in dto)
                        {
                            item.CorrectAnswer = ConvertDigitsToArabicInText(item.CorrectAnswer);
                        }

                        return JsonSerializer.Serialize(dto, _jsonSerializerOptions);
                    }

                case QuestionTypeEnum.Email:
                    {
                        var dto = JsonSerializer.Deserialize<EmailQuestionAnswerDto>(modelAnswer, _jsonSerializerOptions);

                        if (dto == null)
                        {
                            return modelAnswer;
                        }

                        dto.ExpectedSubject = ConvertDigitsToArabicInText(dto.ExpectedSubject);

                        dto.RequiredBodyKeywords = [.. dto.RequiredBodyKeywords.Select(ConvertDigitsToArabicInText)];

                        return JsonSerializer.Serialize(dto, _jsonSerializerOptions);
                    }

                default:
                    return ConvertDigitsToArabicInText(modelAnswer);
            }
        }

        private static QuestionTypeEnum? GetQuestionType(string? questionTypeName)
        {
            if (Enum.TryParse<QuestionTypeEnum>(questionTypeName, out var questionType))
            {
                return questionType;
            }

            return null;
        }

        private static readonly HashSet<long> AllowedBulkCreationQuestionTypes =
        [
            (long)Helper.Enums.QuestionType.Essay,
            (long)Helper.Enums.QuestionType.MCQ,
            (long)Helper.Enums.QuestionType.TrueAndFalse
        ];

        #endregion Helper Methods
    }
}