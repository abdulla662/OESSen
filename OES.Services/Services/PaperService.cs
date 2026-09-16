using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.FlattenedTree.Responses;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Dtos.ItemBankPoint.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.Dtos.SectionDistributionDto.Common;
using OES.Helper.Dtos.SyncQuestions;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Net;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Services.Services
{
    public class PaperService : IPaperService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuestionDistributionValidationService _questionDistributionValidationService;
        private readonly IQuestionService _questionService;
        private readonly IFormQuestionScoreService _formQuestionScoreService;
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IMapper _mapper;
        private readonly IAutoPermissionAssignmentService _autoPermissionAssignmentService;

        public PaperService(IServiceProvider serviceProvider,
                            IQuestionDistributionValidationService questionDistributionValidationService,
                            IQuestionService questionService,
                            IFormQuestionScoreService formQuestionScoreService,
                            ICommonService commonService,
                            FilterParamsValues filterParamsValues,
                            IMapper mapper,
                            IAutoPermissionAssignmentService autoPermissionAssignmentService)
        {
            _serviceProvider = serviceProvider;
            _questionDistributionValidationService = questionDistributionValidationService;
            _questionService = questionService;
            _formQuestionScoreService = formQuestionScoreService;
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _mapper = mapper;
            _autoPermissionAssignmentService = autoPermissionAssignmentService;
        }


        // GET METHODS

        public async Task<IApiResponse> GetAllUserPapersAsync(PaginationSearchModel pagination)
        {
            bool isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            var query = _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(d => isSuperAdmin || d.CreationUser == _filterParamsValues.UserEmail.Trim().ToLower(), Including: nameof(PaperMetadata.Forms))
                .AsNoTracking();

            var userGroups = _filterParamsValues
                .OesUserGroupsAndRoles
               .ConvertAll(g => g.GroupId);

            var paperGroupRepo = _commonService._unitOfWork.Repository<PaperGroups, long>();

            var assignedPaperIds = await paperGroupRepo
                .GetAll(pg => userGroups.Contains(pg.OESGroupId) && !pg.IsDeleted)
                .Select(pg => pg.PaperId)
                .Distinct()
                .ToListAsync();

            var assignedToUserQuery =
                _commonService._unitOfWork.Repository<PaperMetadata, long>()
                .GetAll(p => assignedPaperIds.Contains(p.Id),
                             Including: nameof(PaperMetadata.Forms));

            query = query
                .Union(assignedToUserQuery)
                .Distinct();

            if (!isSuperAdmin)
            {
                query = query.Where(p => assignedPaperIds.Contains(p.Id));
            }

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    var searchKey = pagination.SearchKey.Trim().ToLower();

                    if (pagination.SearchInName && pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey) || x.Description.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInName)
                    {
                        query = query.Where(x => (!string.IsNullOrWhiteSpace(x.Code) && x.Code.ToLower().Contains(searchKey)));
                    }
                    else if (pagination.SearchInDescription)
                    {
                        query = query.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.ToLower().Contains(searchKey));
                    }
                }

                if (pagination.FilterObj is JsonElement jsonElement)
                {
                    var filter = jsonElement.Deserialize<PaperFilterPaginationModel>();

                    if (filter != null)
                    {
                        if (filter.SelectedPaperType.HasValue)
                        {
                            query = query.Where(q => q.Type == filter.SelectedPaperType.Value);
                        }

                        if (filter.SelectedPaperCreationStatus.HasValue)
                        {
                            query = query.Where(q => q.PaperCreationStatus == filter.SelectedPaperCreationStatus.Value);
                        }

                        if (filter.QuestionSelectionType.HasValue)
                        {
                            query = query.Where(q => q.QuestionSelectionType == filter.QuestionSelectionType.Value);
                        }

                        if (filter.IsComplete.HasValue)
                        {
                            if (filter.IsComplete.Value)
                            {
                                query = query.Where(q => q.PaperCreationStatus == PaperCreationStatus.PaperCreated);
                            }
                            else
                            {
                                query = query.Where(q => q.PaperCreationStatus != PaperCreationStatus.PaperCreated);
                            }
                        }
                    }
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                        o.CreationDate < (pagination.ToDate ?? DateTime.Today).AddDays(1));
                }

                var totalItems = await query.CountAsync();

                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = _commonService._mapper.Map<List<UserPapersListDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<UserPapersListDto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                var mappedData = _commonService._mapper.Map<List<UserPapersListDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<UserPapersListDto>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> GetAllPaperGroupsCreatedByUser()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLower() ?? "system";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsTemplate &&
                !g.AutoCreatedForUser &&
                !g.IsPredefined &&
                g.GroupResources.Any() &&
                g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Papers) &&
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

        public async Task<IApiResponse> GetPaperGroupsAsync(long paperId)
        {
            var paperGroups = await _commonService._unitOfWork
                .Repository<PaperGroups, long>()
                .GetAllAsync(x =>
                    x.PaperId == paperId &&
                    !x.OESGroup.IsTemplate,
                    Including: nameof(PaperGroups.OESGroup)
                );

            var dto = new PaperGroupsDto();

            paperGroups.ToList().ForEach(pg => dto.GroupsIds.Add(pg.OESGroupId));

            var owner = paperGroups.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser);

            dto.OwnerGroupId = owner?.OESGroupId;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                dto
            );
        }

        public async Task<IApiResponse> GetAllUserPapersAsync(long scheduleMetadataId)
        {
            var scheduleLanguagesIds = (await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(s => s.Id == scheduleMetadataId, Including: nameof(ScheduleMetadata.Languages)))
                ?.Languages
                ?.Select(l => l.LanguageId) ?? [];

            bool isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            var userEmail = _filterParamsValues.UserEmail;

            var assignedPaperIds = _filterParamsValues.OesUserGroupsAndRoles.ConvertAll(g => g.GroupId);

            var groupPaperIds = new List<long>();
            if (assignedPaperIds.Any())
            {
                groupPaperIds = await _commonService
                    ._unitOfWork
                    .Repository<PaperGroups, long>()
                    .GetAll(pg => assignedPaperIds.Contains(pg.OESGroupId) && !pg.IsDeleted)
                    .Select(pg => pg.PaperId)
                    .Distinct()
                    .ToListAsync();
            }

            var userGroupIdsWithExamView = _filterParamsValues
                .OesUserGroupsAndRoles
                .Where(g => g.GroupRoles.Any(r => r.Name == OesTemplateRoleConstants.ScheduleExamView))
                .Select(g => g.GroupId)
                .ToList();

            var examViewPaperIds = new List<long>();
            if (userGroupIdsWithExamView.Any())
            {
                var linkedScheduleIds = await _commonService
                    ._unitOfWork
                    .Repository<ScheduleGroups, long>()
                    .GetAll(sg => userGroupIdsWithExamView.Contains(sg.OESGroupId) && !sg.IsDeleted)
                    .Select(sg => sg.ScheduleId)
                    .Distinct()
                    .ToListAsync();

                if (linkedScheduleIds.Any())
                {
                    examViewPaperIds = await _commonService
                        ._unitOfWork
                        .Repository<SchedulePaper, long>()
                        .GetAll(sp => linkedScheduleIds.Contains(sp.ScheduleMetadataId) && !sp.IsDeleted)
                        .Select(sp => sp.PaperId)
                        .Distinct()
                        .ToListAsync();
                }
            }

            var userPapers = _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(p =>
                    p.PaperCreationStatus == PaperCreationStatus.PaperCreated &&
                    (!scheduleLanguagesIds.Any() || scheduleLanguagesIds.Contains(p.LanguageId)) &&
                    (
                        isSuperAdmin ||
                        p.CreationUser == userEmail ||
                        groupPaperIds.Contains(p.Id) ||
                        examViewPaperIds.Contains(p.Id)
                    ))
                .OrderByDescending(p => p.CreationDate)
                .ToList();

            if (userPapers.Count == 0)
            {
                return _commonService
                          ._apiResponse
                          .GetApiResponse(CustomCodeStatus.NotFound,
                                          HttpStatusCode.OK,
                                          Resource.PapersNotFoundOrSchedule);
            }

            var mappedUserPapers = _commonService._mapper.Map<List<UserPaperForSchedule>>(userPapers);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null!,
                                mappedUserPapers);
        }

        public async Task<IApiResponse> GetPapersWithoutEquationTemplateAsync()
        {
            var formsWithEquationIds = _commonService._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .Query()
                .Select(pie => pie.FormId);

            var paperFormsQuery = _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .Query();

            var papersWithoutTemplates = await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .Query()
                .Where(p =>
                    p.PaperCreationStatus == PaperCreationStatus.PaperCreated &&
                    !p.IsDeleted &&
                    p.IsActive &&
                    paperFormsQuery
                        .Where(pf => pf.PaperId == p.Id)
                        .Any(pf => !formsWithEquationIds.Contains(pf.Id))
                )
                .Select(p => new GetUserPapersDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type
                })
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            if (papersWithoutTemplates.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No papers found without equation templates"
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                $"Found {papersWithoutTemplates.Count} papers without equation templates",
                papersWithoutTemplates
            );
        }

        public async Task<IApiResponse> GetPapersWithEquationTemplateAsync()
        {
            var paperItembankEquations = _commonService
                ._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .Query();

            var papersWithoutTemplates = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .Query()
                .Where(p => paperItembankEquations.Any(pie => pie.PaperId == p.Id) && !p.IsDeleted && p.IsActive) // TODO: p.PaperCreationStatus == PaperCreationStatus.PaperCreated
                .OrderByDescending(p => p.CreationDate)
                .Select(p => new GetUserPapersDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type
                })
                .ToListAsync();

            if (papersWithoutTemplates.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No papers found with equation templates"
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                $"Found {papersWithoutTemplates.Count} papers with equation templates",
                papersWithoutTemplates
            );
        }

        public async Task<IApiResponse> GetAllPaperMetadataTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<PaperMetadataTemplate, long>()
                .GetAll()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKeyLower = paginationSearchModel.SearchKey.ToLower();

                query = query.Where(a => a.Name.ToLower().Contains(searchKeyLower));
            }

            if (paginationSearchModel.FromDate.HasValue)
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);

            if (paginationSearchModel.ToDate.HasValue)
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);

            if (!(await query.AnyAsync()))
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            var pageIndex = Math.Max(0, paginationSearchModel.PageIndex);

            var pageSize = Math.Max(1, paginationSearchModel.PageSize);

            var paginatedTemplates = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            if (!paginatedTemplates.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMoreTemplatesFound);

            var templateDtos = paginatedTemplates.ConvertAll(q => _mapper.Map<PaperMetadataTemplateDto>(q));

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.TemplateFound,
                                new CustomTableData<PaperMetadataTemplateDto>(templateDtos, totalRecords));
        }

        public async Task<IApiResponse> GetPaperMetaDataAsync(long paperId)
        {
            const string includes = $"{nameof(PaperMetadata.Subjects)}," +
                                    $"{nameof(PaperMetadata.PaperStageCategoryDecisionPaths)}," +
                                    $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Stages)}.{nameof(Stage.CategoryDecisionPaths)}," +
                                    $"{nameof(PaperMetadata.PaperGroups)}.{nameof(PaperGroups.OESGroup)}";

            var paperMetadata = await _commonService
               ._unitOfWork
               .Repository<PaperMetadata, long>()
               .GetObjAsync(p => p.Id == paperId, includes);

            if (paperMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperNotFound);
            }

            paperMetadata.MarkingScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(e => e.PaperId == paperMetadata.Id);

            var mappingDto = _mapper.Map<GetPaperMetadataResponseDto>(paperMetadata);

            mappingDto.OESGroupDtos = paperMetadata.PaperGroups
                .Where(pg => pg.OESGroup != null)
                .Select(pg => new GetOESGroupDto
                {
                    Id = pg.OESGroup.Id,
                    Name = pg.OESGroup.Name,
                    AutoCreatedForUser = pg.OESGroup.AutoCreatedForUser
                })
                .ToList() ?? [];

            mappingDto.ScoreSchemaType = paperMetadata.MarkingScheme?.ScoreType ?? default;

            if (paperMetadata.Type == PaperType.Adaptive)
            {
                mappingDto.StageCount = paperMetadata.StageCount;

                mappingDto.AdaptiveSubtype = paperMetadata.AdaptiveSubtype;

                mappingDto.IsStepPlus = paperMetadata.IsStepPlus;

                mappingDto.CategoryStagePaths = [];

                mappingDto.CategoryFixedDPaths = [];

                if (paperMetadata.DPathCalculationMode == DPathCalculationMode.FullyManual)
                {
                    var defaultForm = paperMetadata.Forms?.FirstOrDefault();

                    if (defaultForm.Stages?.Count > 0)
                    {
                        var distinctCategoryIds = defaultForm.Stages
                            .SelectMany(s => s.CategoryDecisionPaths)
                            .Where(p => p.DecisionPathValue.HasValue)
                            .Select(p => p.QuestionCategoryId)
                            .Distinct()
                            .ToList();

                        var sortedStages = defaultForm.Stages.OrderBy(s => s.Order).ToList();

                        foreach (var categoryId in distinctCategoryIds)
                        {
                            var pathsForCategory = new List<decimal>();

                            foreach (var stage in sortedStages)
                            {
                                var pathEntity = stage.CategoryDecisionPaths.FirstOrDefault(x => x.QuestionCategoryId == categoryId &&
                                                                                                 x.DecisionPathValue.HasValue);

                                if (pathEntity != null)
                                {
                                    pathsForCategory.Add(pathEntity.DecisionPathValue.Value);
                                }
                            }

                            if (pathsForCategory.Count > 0)
                            {
                                mappingDto.CategoryStagePaths.Add(categoryId, pathsForCategory);
                            }
                        }
                    }
                }
                else if (paperMetadata.DPathCalculationMode == DPathCalculationMode.ManualFinalScore)
                {
                    var categoryLevelPaths = paperMetadata
                        .PaperStageCategoryDecisionPaths?
                        .Where(p => !p.StageId.HasValue && p.FixedDPath.HasValue)
                        .ToList();

                    if (categoryLevelPaths?.Count > 0)
                    {
                        foreach (var path in categoryLevelPaths)
                        {
                            mappingDto.CategoryFixedDPaths.Add(path.QuestionCategoryId, path.FixedDPath.Value);
                        }
                    }
                }
            }

            if (paperMetadata.Type == PaperType.Adaptive && !string.IsNullOrWhiteSpace(paperMetadata.AdaptiveCategoryExecutionOrder))
            {
                mappingDto.AdaptiveCategoryExecutionOrder = JsonConvert.DeserializeObject<List<long>>(paperMetadata.AdaptiveCategoryExecutionOrder) ?? [];
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              mappingDto);
        }

        public async Task<IApiResponse> GetPaperDataForViewByIdAsync(long id)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == id, Including: "DifficultyProfile,Language");

            if (paper != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.FetchSuccess,
                                    _commonService._mapper.Map<PaperDataViewResponseDto>(paper));
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.PaperNotFound);
        }

        public async Task<IApiResponse> GetPaperDurationByIdAsync(long id)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == id);

            if (paper != null)
            {
                var duration = new PaperDurationResponseDto
                {
                    Duration = (long)paper.Duration
                };

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DurationFetchSuccess,
                                    duration);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.PaperNotFound);
        }

        public async Task<IApiResponse> GetCurrentPaperCreationStatusAsync(long paperId)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.NotFound,
                                    Resource.PaperNotFound);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null!,
                                new GetCurrentPaperCreationStatusResponseDto(paper.PaperCreationStatus));
        }

        public async Task<IApiResponse> GetManuallySelectedQuestionsForUpdateAsync(long paperId, long? formId = null)
        {
            const string includes = $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionDetails)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionType)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(QuestionMetadata.SubQuestions)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint)}.{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint.ItemBank)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.DifficultyLevel)},{nameof(ManualPaperItemBankQuestionSection.Section)}";

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperNotFound);
            }

            var query = _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAll(x => x.ItemBankPoint.PaperId == paperId, null, includes);

            if (formId.HasValue)
            {
                query = query.Where(x => x.SectionId == null || x.Section.FormId == formId.Value);
            }
            else
            {
                query = query.Where(x => x.SectionId == null);

            }

            var totalUsedCount = await _commonService
                        ._unitOfWork
                        .Repository<ManualPaperItemBankQuestionSection, long>()
                        .GetAll(x =>
                            x.ItemBankPoint.PaperId == paperId &&
                            x.PaperQuestionStatus == PaperQuestionStatus.Used)
                        .SumAsync(x =>
                            x.QuestionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension
                                ? x.QuestionMetadata.SubQuestions.Count
                                : 1);

            // IMPORTANT NOTICE: If this throws an exception, it may means the selected questions metadata don't have question details,
            // so you must fetch first paginated questions that has the status APPROVED in the third step (Manual Questions Selection) of the paper stepper.
            var selectedQuestions = await query
                .Select(q => new ManualQuestionsPaginationResponseDto(
                    q.QuestionMetaDataId,
                    q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).Body : "",
                    q.QuestionMetadata.Code,
                    q.QuestionMetadata.QuestionType.Name,
                    q.ItemBankPointId,
                    q.ItemBankPoint.ItemBank.Name,
                    q.DifficultyLevelId,
                    q.DifficultyLevel.Name,
                    q.PaperQuestionStatus,
                    q.Id,
                    q.SectionId,
                    q.Section == null ? null : q.Section.FormId,
                    q.QuestionMetadata.QuestionType.Name == nameof(Helper.Enums.QuestionType.Comprehension)
                        ? (q.QuestionMetadata.SubQuestions.Count < 1 ? 1 : q.QuestionMetadata.SubQuestions.Count)
                        : 1))
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              new ManualQuestionsResultDto
                                                              {
                                                                  Questions = selectedQuestions,
                                                                  TotalUsedCount = totalUsedCount
                                                              });
        }

        public async Task<IApiResponse> GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(long paperId)
        {
            const string includes = $"{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint)}.{nameof(PaperItemBankPoint.ItemBank)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionDetails)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionType)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(QuestionMetadata.SubQuestions)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.DifficultyLevel)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.Section)}";

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperNotFound);
            }

            var paperForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(x => x.PaperId == paperId, Including: nameof(PaperForm.Sections), asNoTracking: true);

            var paperFormsDtos = paperForms
                .Select(x => new FormDetailsDto(
                    x.Id,
                    x.Name,
                    x.Code,
                    x.Description
                ))
                .ToList();

            var paperSectionsDtos = paperForms
                .SelectMany(x => x.Sections)
                .Select(x => new FormSectionResponseDto(
                    x.Id,
                    x.Name,
                    x.FormId
                ))
                .ToList();

            var generatedFormQuestions = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAll(x => x.Form.PaperId == paperId)
                .Select(x => new { x.FormId, x.QuestionId, x.Score })
                .ToListAsync();

            // IMPORTANT NOTICE: If this throws an exception, it may means the selected questions metadata don't have question details,
            // so you must fetch first paginated questions that has the status APPROVED in the third step (Manual Questions Selection) of the paper stepper.
            var selectedQuestionsDtos = await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAll(x => x.ItemBankPoint.PaperId == paperId && x.PaperQuestionStatus == PaperQuestionStatus.Used, null, includes)
                .Select(q => new ManuallySelectedQuestionsResponseDto()
                {
                    Id = q.Id,
                    QuestionMetadataId = q.QuestionMetaDataId,
                    Body = q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).Body : "",
                    Code = q.QuestionMetadata.Code,
                    LanguageId = q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).LanguageId : 0L,
                    DeltaValue = q.QuestionMetadata.Delta,
                    ItemBankId = q.ItemBankPoint.ItemBankId,
                    ItemBankName = q.ItemBankPoint.ItemBank.Name,
                    DifficultyLevelId = q.DifficultyLevelId,
                    DifficultyLevelName = q.DifficultyLevel.Name,
                    SectionName = q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.Name,
                    SectioningIdentifier = q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.SectioningIdentifier,
                    PaperQuestionStatus = q.PaperQuestionStatus,
                    FormId = q.SectionId == null ? null : q.Section.FormId,
                    SectionId = q.SectionId,
                    SubQuestionsCount = q.QuestionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension ? q.QuestionMetadata.SubQuestions.Count : 1,
                    QuestionType = (Helper.Enums.QuestionType)q.QuestionMetadata.QuestionTypeId,
                    SubQuestions = q.QuestionMetadata.SubQuestions
                        .Where(sub => sub.QuestionDetails.Any(qd => qd.LanguageId == paper.LanguageId))
                        .Select(sub => new ManuallySelectedQuestionsResponseDto
                        {
                            Id = sub.Id,
                            QuestionMetadataId = sub.Id,
                            Body = sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).Body : "",
                            Code = sub.Code,
                            LanguageId = sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).LanguageId : 0L,
                            DeltaValue = sub.Delta,
                            ItemBankId = q.ItemBankPoint.ItemBankId,
                            ItemBankName = q.ItemBankPoint.ItemBank.Name,
                            DifficultyLevelId = q.DifficultyLevelId,
                            DifficultyLevelName = q.DifficultyLevel.Name,
                            QuestionType = (Helper.Enums.QuestionType)sub.QuestionTypeId,
                            SectionName = q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.Name,
                            SectioningIdentifier = q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.SectioningIdentifier,
                            PaperQuestionStatus = q.PaperQuestionStatus,
                            FormId = q.SectionId == null ? null : q.Section.FormId,
                            SectionId = q.SectionId,
                            SubQuestionsCount = 1
                        }).ToList()
                })
                .ToListAsync();

            var scoreLookup = generatedFormQuestions.ToDictionary(x => (x.FormId, x.QuestionId), x => x.Score);

            foreach (var question in selectedQuestionsDtos)
            {
                scoreLookup.TryGetValue((question.FormId, question.QuestionMetadataId), out var score);

                question.Mark = score;
            }

            var collectivePaperFormSectionQuestionResponseDto = new CollectivePaperFormSectionQuestionResponseDto(
                paperFormsDtos,
                paperSectionsDtos,
                selectedQuestionsDtos
            );

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              collectivePaperFormSectionQuestionResponseDto);
        }

        public async Task<ApiResponse> GetPaperManualSectionsWithQuestionsAsync(long paperId)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.NotFound,
                                    Resource.PaperNotFound);
            }

            var paperManualSections = await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .GetAll(p => p.PaperId == paperId)
                .Select(s => new SectionResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    SectioningIdentifier = s.SectioningIdentifier,
                    IsRestrictedTime = s.IsRestrictedTime,
                    TimeInMinutes = s.TimeInMinutes,
                    IsRandom = s.IsRandom,
                    InstructionSectionTemplateId = s.InstructionSectionTemplateId,
                    OrderId = s.OrderId
                })
                .ToListAsync();

            const string includes = $"{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint)}.{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint.ItemBank)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionDetails)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata.QuestionType)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}.{nameof(QuestionMetadata.SubQuestions)}," +
                                    $"{nameof(ManualPaperItemBankQuestionSection.Section)},{nameof(ManualPaperItemBankQuestionSection.DifficultyLevel)}";

            // IMPORTANT NOTICE: If this throws an exception, it may means the selected questions metadata don't have question details,
            // so you must fetch first paginated questions that has the status APPROVED in the third step (Manual Questions Selection) of the paper stepper.
            var paperManualQuestions = await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAll(x => x.ItemBankPoint.PaperId == paperId && x.PaperQuestionStatus == PaperQuestionStatus.Used, null, includes)
                .Select(q => new ManuallySelectedQuestionsResponseDto(
                    q.Id,
                    q.QuestionMetaDataId,
                    q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).Body : "",
                    q.QuestionMetadata.Code,
                    q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? q.QuestionMetadata.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).LanguageId : 0L,
                    q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.SectioningIdentifier,
                    q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.Name,
                    q.ItemBankPoint.ItemBankId,
                    q.ItemBankPoint.ItemBank.Name,
                    q.DifficultyLevelId,
                    q.DifficultyLevel.Name,
                    null,
                    q.QuestionMetadata.Delta,
                    q.PaperQuestionStatus,
                    q.SectionId == null ? null : q.Section.FormId,
                    q.SectionId,
                    q.QuestionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension ? q.QuestionMetadata.SubQuestions.Count : 1,
                    (Helper.Enums.QuestionType)q.QuestionMetadata.QuestionTypeId,
                    q.QuestionMetadata.SubQuestions
                        .Where(sub => sub.QuestionDetails.Any(qd => qd.LanguageId == paper.LanguageId))
                        .Select(sub => new ManuallySelectedQuestionsResponseDto(
                            sub.Id,
                            sub.Id,
                            sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).Body : "",
                            sub.Code,
                            sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId) != null ? sub.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId).LanguageId : 0L,
                            q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.SectioningIdentifier,
                            q.SectionId == null ? MiscConstants.ManualQuestionsSelectionPool : q.Section.Name,
                            q.ItemBankPoint.ItemBankId,
                            q.ItemBankPoint.ItemBank.Name,
                            q.DifficultyLevelId,
                            q.DifficultyLevel.Name,
                            null,
                            sub.Delta,
                            q.PaperQuestionStatus,
                            q.SectionId == null ? null : q.Section.FormId,
                            q.SectionId,
                            1, // sub-question always = 1
                            (Helper.Enums.QuestionType)sub.QuestionTypeId,
                            null
                        )).ToList()
                )).ToListAsync();

            var response = await _formQuestionScoreService.GetQuestionsRelationsForManualPaperAsync(paperId);

            var paperForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(x => x.PaperId == paperId && !x.IsDeleted && x.IsActive);

            var paperFormsDtos = paperForms.Select(x => new FormDetailsDto(
                x.Id,
                x.Name,
                x.Code,
                x.Description
            ))
            .ToList();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var questionSectionFormDist = (List<QuestionForPaperDto>)response.Data ?? [];

                var paperManualSectionsWithQuestionsResponseDto = new GetPaperManualSectionsWithQuestionsResponseDto(
                    paperFormsDtos,
                    paperManualSections,
                    paperManualQuestions,
                    questionSectionFormDist
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null!,
                                    paperManualSectionsWithQuestionsResponseDto);
            }
            else
            {
                var paperManualSectionsWithQuestionsResponseDto = new GetPaperManualSectionsWithQuestionsResponseDto(
                    paperFormsDtos,
                    paperManualSections,
                    paperManualQuestions,
                    []
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null!,
                                    paperManualSectionsWithQuestionsResponseDto);
            }
        }

        public async Task<IApiResponse> GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(long paperId)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(p => p.Id == paperId)
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.LanguageId,
                    p.DifficultyProfileId
                }).FirstOrDefaultAsync();

            if (paper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.NotFound,
                                    Resource.PaperNotFound);
            }

            var paperAutoSections = await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .GetAll(p => p.PaperId == paperId)
                .AsNoTracking()
                .Select(s => new DistributionSectionResponseDto
                (
                    s.Name,
                    s.IsRestrictedTime,
                    s.TimeInMinutes,
                    s.IsRandom,
                    s.InstructionSectionTemplateId,
                    s.OrderId
                ))
                .ToListAsync();

            var paperAutoQuestionsRecords = await _commonService
                ._unitOfWork
                .Repository<AutoPaperItemBankQuestionSection, long>()
                .Query()
                .Where(x => x.ItemBankPoint.PaperId == paperId)
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.QuestionIds,
                    x.SelectedCount,
                    x.SubQuestionCount,
                    x.ItemBankPoint.ItemBankId,
                    ItemBankName = x.ItemBankPoint.ItemBank.Name,
                    QuestionTypeId = x.QuestionTypeID,
                    QuestionTypeName = x.QuestionType.Name,
                    SectionName = x.Section != null ? x.Section.Name : null,
                    DifficultyLevelId = x.DifficultyLevelID,
                    DifficultyLevelName = x.DifficultyLevel != null ? x.DifficultyLevel.Name : Resource.UnKnown
                }).ToListAsync();

            var questionIdsLookup = paperAutoQuestionsRecords.ToDictionary(r => r.Id, r => string.IsNullOrWhiteSpace(r.QuestionIds) ? [] : JsonSerializer.Deserialize<List<long>>(r.QuestionIds) ?? []);

            var allQuestionIds = questionIdsLookup.Values
                .SelectMany(ids => ids)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            // Fetch all questions by questions Ids:
            var questionsLookup = new Dictionary<long, QuestionMetadataDto>();
            if (allQuestionIds.Count > 0)
            {
                var response = await _questionService.GetAllQuestionsByIds(allQuestionIds);

                if (response.StatusCode == HttpStatusCode.OK &&
                    response.Data is List<QuestionMetadataDto> questionsMetadata)
                {
                    questionsLookup = questionsMetadata.ToDictionary(q => q.Id, q => q);
                }
            }

            var difficultyLevelsLookup = paperAutoQuestionsRecords
                .GroupBy(x => x.DifficultyLevelId)
                .ToDictionary(dl => dl.Key, dl => dl.First().DifficultyLevelName);

            var paperAutoQuestions = paperAutoQuestionsRecords
                .GroupBy(q => new
                {
                    q.ItemBankId,
                    q.ItemBankName,
                    q.QuestionTypeId,
                    q.QuestionTypeName,
                    q.SectionName
                })
                .Select(group =>
                {
                    var manualQuestions = group
                    .SelectMany(r =>
                    {
                        var ids = questionIdsLookup[r.Id];

                        return ids
                        .Where(id => questionsLookup.ContainsKey(id))
                        .Select(id =>
                        {
                            var q = questionsLookup[id];
                            difficultyLevelsLookup.TryGetValue(q.DifficultyLevelId, out var difficultyName);
                            return new QuestionSimpleDataDto
                            {
                                Id = q.Id,
                                Code = q.Code,
                                Body = q.Body,
                                DifficultyLevelId = q.DifficultyLevelId,
                                DifficultyLevelName = difficultyName ?? Resource.UnKnown,
                                IsSelected = true,
                                SubQuestionsCount = (int)(r.SubQuestionCount > 0 ? r.SubQuestionCount : 1)
                            };
                        });
                    })
                    .ToList();

                    var difficultyBreakdown = group
                        .GroupBy(x => x.DifficultyLevelName)
                        .ToDictionary(g => g.Key, g => g.Sum(q => q.SelectedCount));

                    var subQuestionDistributions = group
                        .Where(x => x.SubQuestionCount > 0 && (string.IsNullOrWhiteSpace(x.QuestionIds) || x.QuestionIds == "[]"))
                        .GroupBy(x => x.DifficultyLevelName)
                        .ToDictionary(
                            g => g.Key,
                            g => g
                            .GroupBy(x => x.SubQuestionCount)
                            .Select(x => new ComprehensionDistributionItem(
                                SubQuestionsCount: (int)x.Key,
                                Count: x.Sum(i => i.SelectedCount)
                            ))
                            .OrderBy(x => x.SubQuestionsCount)
                            .ToList()
                        );

                    return new MixedSelectedQuestionsNodeDto(
                        totalManualQuestions: manualQuestions,
                        itemBankId: group.Key.ItemBankId,
                        itemBankName: group.Key.ItemBankName,
                        questionTypeId: group.Key.QuestionTypeId,
                        questionTypeName: group.Key.QuestionTypeName,
                        sectionName: group.Key.SectionName,
                        currentQuestionsCount: group.Sum(q => q.SelectedCount),
                        difficultyLevelsBreakdown: [],
                        assignedDifficultyLevelsBreakdown: difficultyBreakdown,
                        subQuestionDistributions: subQuestionDistributions,
                        languageId: paper.LanguageId,
                        difficultyProfileId: paper.DifficultyProfileId ?? 0
                    );
                })
                .ToList();

            var paperAutoSectionsWithQuestionsDistributionsResponseDto = new GetPaperAutoSectionsWithQuestionsDistributionsResponseDto(paperAutoSections, paperAutoQuestions);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null!,
                                paperAutoSectionsWithQuestionsDistributionsResponseDto);
        }

        public async Task<IApiResponse> GetItemBanksFlattenedTreeNodesAsync(long paperId)
        {
            var flattenedNodes = await _commonService
                ._unitOfWork
                .Repository<FlattenedTreeNodeView, long>()
                .GetAll(o => o.PaperId == paperId)
                .AsNoTracking()
                .ToListAsync();

            if (flattenedNodes.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    string.Format(Resource.NoFlattenedTreeFound, paperId));
            }

            var result = new GetPaperItemBanksResponseDto
            {
                PaperId = paperId,
                ItemBanks = [.. flattenedNodes
                .GroupBy(node => new { node.ItemBankId, node.ItemBankName })
                .Select(itemBankGroup => new GetItemBankResponseDto
                {
                    ItemBankId = itemBankGroup.Key.ItemBankId,
                    ItemBankName = itemBankGroup.Key.ItemBankName,
                    QuestionTypes = [.. itemBankGroup
                        .GroupBy(node => new { node.QuestionTypeId, node.QuestionTypeName })
                        .Select(questionTypeGroup => new GetQuestionTypeResponseDto
                        {
                            QuestionTypeId = questionTypeGroup.Key.QuestionTypeId,
                            QuestionTypeName = questionTypeGroup.Key.QuestionTypeName,
                            TypeQuestionsCount = questionTypeGroup.Sum(n => n.QuestionTypeCount),
                            DifficultyLevels = [.. questionTypeGroup
                                .Where(x=> !x.AllowInstantResultPaper || x.IsAutoCorrectableQuestion)
                                .GroupBy(node => new { node.DifficultyLevelId, node.DifficultyLevelName })
                                .Select(difficultyGroup => new GetDifficultyLevelResponseDto
                                {
                                    DifficultyLevelId = difficultyGroup.Key.DifficultyLevelId,
                                    DifficultyLevelName = difficultyGroup.Key.DifficultyLevelName,
                                    QuestionCount = difficultyGroup.Sum(node => node.QuestionCount)
                                })]
                        })]
                })]
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                string.Format(Resource.FlattenedTreefOrPaperRetrievedSuccessfully, paperId),
                                result);
        }

        public async Task<IApiResponse> GetAutoSelectedQuestionsForMarkingSchemeAsync(long paperId)
        {
            const string includes = $"{nameof(AutoPaperItemBankQuestionSection.ItemBankPoint)}," +
                                    $"{nameof(AutoPaperItemBankQuestionSection.DifficultyLevel)}," +
                                    $"{nameof(AutoPaperItemBankQuestionSection.QuestionType)}," +
                                    $"{nameof(AutoPaperItemBankQuestionSection.Section)}," +
                                    $"{nameof(AutoPaperItemBankQuestionSection.ItemBankPoint)}.{nameof(PaperItemBankPoint.ItemBank)}";

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperNotFound);
            }

            var sectionRecords = await _commonService
                ._unitOfWork
                .Repository<AutoPaperItemBankQuestionSection, long>()
                .GetAll(x => x.Section.PaperId == paperId, null, includes)
                .ToListAsync();

            var formQuestions = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAll(fq => fq.Form.PaperId == paperId && fq.QuestionId != null)
                .Select(fq => new { fq.Question.ItemBankId, fq.Question.DifficultyLevelId, fq.Question.QuestionTypeId, fq.Question.Code })
                .ToListAsync();

            var questionCodesLookup = formQuestions
                .GroupBy(fq => (fq.ItemBankId, fq.DifficultyLevelId, fq.QuestionTypeId))
                .ToDictionary(g => g.Key, g => g.Select(fq => fq.Code).Distinct().ToList());

            var selectedQuestions = sectionRecords.ConvertAll(q => new AutoSelectedQuestionsResponseDto()
            {
                Id = q.Id,
                QuestionTypeId = q.QuestionTypeID,
                QuestionTypeName = q.QuestionType.Name,
                DifficultyLevelId = q.DifficultyLevelID,
                DifficultyLevelName = q.DifficultyLevel.Name,
                ItemBankId = q.ItemBankPoint.ItemBank.Id,
                ItemBankName = q.ItemBankPoint.ItemBank.Name,
                SelectedCount = q.SelectedCount,
                SubQuestionCount = q.SubQuestionCount,
                SectionName = q.Section.Name,
                QuestionCodes = questionCodesLookup.TryGetValue((q.ItemBankPoint.ItemBank.Id, q.DifficultyLevelID, q.QuestionTypeID), out var codes)
                    ? [.. codes.Take((int)q.SelectedCount)]
                    : [],
            });

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.AutoSelectedQuestionsRetrievedSuccessfully,
                                                              selectedQuestions);
        }

        public async Task<IApiResponse> GetPaperDifficultyLevelsAsync(long paperId)
        {
            var PaperMeta = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(o => o.Id == paperId, Including: $"{nameof(PaperMetadata.ItemBanksPoints)},{nameof(PaperItemBankPoint.ItemBank)},{nameof(ItemBank.QuestionMetadata)}");

            if (PaperMeta != null)
            {
                var questionExhaustionCounts = PaperMeta
                    .ItemBanksPoints
                    .SelectMany(ibp => ibp.ItemBank.QuestionMetadata)
                    .Select(q => q.QuestionsExhaustionCount)
                    .ToList();

                var DifficultyLevels = _commonService
                         ._unitOfWork
                         .Repository<DifficultyLevel, long>()
                         .GetAll(o => o.DifficultyProfileId == PaperMeta.DifficultyProfileId)
                         .Select(d => new GetListedDifficultyLevelResponseDto { Id = d.Id, Name = d.Name })
                         .ToList();

                var itemBanksIds = _commonService
                        ._unitOfWork
                        .Repository<PaperItemBankPoint, long>()
                        .GetAll(o => o.PaperId == paperId)
                        .Select(d => d.ItemBankId)
                        .ToList();

                var QuestionCount = _commonService
                       ._unitOfWork
                       .Repository<QuestionTypeCountView, long>()
                       .GetAll(d => itemBanksIds.Contains(d.ItemBankId))
                       .Select(d => new QuestionTypeCountViewRequestDto
                       {
                           ItemBankId = d.ItemBankId,
                           QuestionType = d.QuestionType,
                           DifficultyLevel = d.DifficultyLevel,
                           QuestionCount = d.QuestionCount
                       })
                       .ToList();

                var result = new DiffcultyLevelsPerProfileResponseDto
                {
                    DifficultyLevels = DifficultyLevels,
                    count = QuestionCount,
                    QuestionCount = questionExhaustionCounts.Sum()
                };

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null!,
                                                                  result);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.PaperIdIsNotCorrect,
                                                                  null!);
            }
        }

        public async Task<IApiResponse> GetAllItemBanksFromItemBankPointsAsync(long paperId)
        {
            var ItemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(o => o.PaperId == paperId, null, Including: nameof(PaperItemBankPoint.ItemBank))

                .Select(it => new ItemBanksFromItemBankPointResponseDto
                {
                    Id = it.ItemBank.Id,
                    Name = it.ItemBank.Name,
                    ItemBankSignature = it.ItemBank.ItemBankSignature
                }).ToListAsync();

            var allItemBanks = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetAll()
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in ItemBankPoints)
            {
                List<string> path = [];

                var current = allItemBanks.FirstOrDefault(x => x.Id == dto.Id);

                while (current != null)
                {
                    path.Insert(0, current.Name);
                    current = allItemBanks.FirstOrDefault(x => x.Id == current.ParentId);
                }

                dto.FullPath = string.Join(" > ", path);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              ItemBankPoints);
        }

        public async Task<IApiResponse> GetPaperMetadataTemplateByIdAsync(long PaperMetadataTemplateId)
        {
            var template = await _commonService._unitOfWork.Repository<PaperMetadataTemplate, long>().GetByIdAsync(PaperMetadataTemplateId);

            if (template == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            var deserializedData = JsonConvert.DeserializeObject<AddOrUpdatePaperMetadataRequestDto>(template.Data);

            if (deserializedData == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, Resource.InvalidDataProvided);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.TemplateFound, deserializedData);
        }

        public async Task<IApiResponse> CopyPaperAsync(long paperId)
        {
            var paperMetaData = await _commonService._unitOfWork
                                                    .Repository<PaperMetadata, long>()
                                                    .GetObjAsync(result => result.Id == paperId);

            if (paperMetaData == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            // Define dynamic Includes based on paper type:
            string includes = $"{nameof(PaperMetadata.MarkingScheme)},{nameof(PaperMetadata.Subjects)}";

            if (paperMetaData.Type == PaperType.Standard)
            {
                includes += $",{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.FormQuestions)}," +
                            $"{nameof(PaperMetadata.Sections)}," +
                            $"{nameof(PaperMetadata.ItemBanksPoints)}";

                if (paperMetaData.QuestionSelectionType == QuestionSelectionType.Manual)
                {
                    includes += $".{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)}";
                }
                else if (paperMetaData.QuestionSelectionType == QuestionSelectionType.Auto)
                {
                    includes += $".{nameof(PaperItemBankPoint.AutoPaperItemBankQuestionSections)}";
                }
            }
            else if (paperMetaData.Type == PaperType.Adaptive)
            {
                includes += $",{nameof(PaperMetadata.PaperFormBlocks)}.{nameof(PaperFormBlock.AdaptiveSection)}," +
                            $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Stages)}.{nameof(Stage.AdaptiveSections)}," +
                            $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Stages)}.{nameof(Stage.CategoryDecisionPaths)}," +
                            $"{nameof(PaperMetadata.PaperStageCategoryDecisionPaths)}";
            }

            // Fetch PaperMetadata with specific Includes:
            paperMetaData = await _commonService._unitOfWork
                                                .Repository<PaperMetadata, long>()
                                                .GetObjAsync(result => result.Id == paperId, Including: includes);

            if (paperMetaData == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFoundAfterApplyingIncludes
                );
            }

            var copiedPaper = _mapper.Map<PaperMetadata>(paperMetaData);

            await _commonService._unitOfWork.Repository<PaperMetadata, long>().AddAsync(copiedPaper);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperCopiedSuccessfully
            );
        }

        public async Task<ApiResponse> IsPaperAssignedToScheduleAsync(long paperId)
        {
            bool isAssigned = await _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .AnyAsync(sp => sp.PaperId == paperId);

            return _commonService._apiResponse.GetApiResponse(
                isAssigned ? CustomCodeStatus.Success : CustomCodeStatus.Failure,
                HttpStatusCode.OK,
                null
            );
        }


        // ADD METHODS

        public async Task<IApiResponse> AddPaperMetaDataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto)
        {
            var validationResult = await ValidatePaperMetadataRequestAsync(paperMetadataCreationRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message);
            }

            var paper = _commonService._mapper.Map<PaperMetadata>(paperMetadataCreationRequestDto);

            paper.StageCount = paper.Type == PaperType.Adaptive ? paperMetadataCreationRequestDto.StageCount : 0;

            paper.AdaptiveSubtype = paper.Type == PaperType.Adaptive ? paperMetadataCreationRequestDto.AdaptiveSubtype : AdaptivePaperSubtype.None;

            paper.IsStepPlus = paper.Type == PaperType.Adaptive && paper.AdaptiveSubtype == AdaptivePaperSubtype.STEP && paperMetadataCreationRequestDto.IsStepPlus;

            paper.PaperCreationStatus = PaperCreationStatus.MetadataAdded;

            paper.PaperStatus = AvailabilityStatus.Active;

            if (paper.Type == PaperType.Adaptive)
            {
                HandleAdaptivePaperFormAddition(paper, paperMetadataCreationRequestDto);
            }

            if (paperMetadataCreationRequestDto.AdaptiveCategoryExecutionOrder?.Count > 0)
            {
                paper.AdaptiveCategoryExecutionOrder = JsonConvert.SerializeObject(paperMetadataCreationRequestDto.AdaptiveCategoryExecutionOrder);
            }
            else
            {
                paper.AdaptiveCategoryExecutionOrder = null;
            }

            await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .AddAsync(paper);

            if (await _commonService._unitOfWork.Complete() <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.Failedtosavepapermetadata
                );
            }

            _ = Guid.TryParse(_filterParamsValues.UserId, out Guid parsedUserId);

            var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isSuperAdmin)
            {
                await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(
                    new AutoPermissionAssignmentRequest
                    {
                        EntityId = paper.Id,
                        EntityName = paper.Id.ToString(),
                        ResourceType = ResourceType.Papers,
                        UserId = parsedUserId,
                        AdditionalGroupIds = paperMetadataCreationRequestDto.OESGroupDtos?.Select(x => x.Id).ToList(),
                        EntityGroupType = typeof(PaperGroups)
                    });
            }

            // Handling the rest of steps after saving paper metadata till step 4:
            if (paper.Type == PaperType.Standard && paper.UsesExcelQuestionsImport && paperMetadataCreationRequestDto.UploadedQuestions != null)
            {
                var (IsValid, Message) = await HandleManualPaperFormAdditionUsingExcelFileAsync(paper, paperMetadataCreationRequestDto);

                if (!IsValid)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.BadRequest,
                        $"{Resource.PaperMetadataSavedSuccessfullyBut} {Message}"
                    );
                }
            }

            var paperLanguage = await _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetObjAsync(l => l.Id == paper.LanguageId);

            var paperMetadataResponseDto = new AddOrUpdatePaperMetadataResponseDto(
                paper.Id,
                paper.Name,
                paper.Type,
                paper.QuestionSelectionType,
                paper.PaperCreationStatus,
                paper.QuestionsCount,
                paper.Duration,
                paper.TotalMarks,
                paper.DifficultyProfileId,
                paper.LanguageId,
                paperLanguage?.Name,
                paper.OutputFormsCount,
                paper.AllowInstantResult,
                paper.UsesExcelQuestionsImport,
                paper.Type == PaperType.Adaptive ? paper.StageCount : 0,
                paper.QuestionDistributionTypeInForm,
                paper.Type == PaperType.Adaptive ? paper.DPathCalculationMode : DPathCalculationMode.None,
                paper.AdaptiveSubtype,
                paper.IsStepPlus
            );

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperSavedSuccessfully,
                paperMetadataResponseDto
            );
        }

        public async Task<ApiResponse> AddManualFormUsingExcelSheetAsync(long paperId, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto)
        {
            var targetPaper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId, Including: nameof(PaperMetadata.Language));

            if (targetPaper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.NotFound,
                        Resource.PaperNotFound
                    );
            }

            targetPaper.OutputFormsCount = addOrUpdatePaperMetadataRequestDto.OutputFormsCount;

            await _commonService._unitOfWork.Complete();

            var (IsValid, Message) = await HandleManualPaperFormAdditionUsingExcelFileAsync(targetPaper, addOrUpdatePaperMetadataRequestDto);

            if (!IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Message
                );
            }

            var paperMetadataResponseDto = new AddOrUpdatePaperMetadataResponseDto(targetPaper.Id,
                                                                                   targetPaper.Name,
                                                                                   targetPaper.Type,
                                                                                   targetPaper.QuestionSelectionType,
                                                                                   targetPaper.PaperCreationStatus,
                                                                                   targetPaper.QuestionsCount,
                                                                                   targetPaper.Duration,
                                                                                   targetPaper.TotalMarks,
                                                                                   targetPaper.DifficultyProfileId,
                                                                                   targetPaper.LanguageId,
                                                                                   targetPaper.Language.Name,
                                                                                   targetPaper.OutputFormsCount,
                                                                                   targetPaper.AllowInstantResult,
                                                                                   targetPaper.UsesExcelQuestionsImport,
                                                                                   targetPaper.StageCount,
                                                                                   targetPaper.QuestionDistributionTypeInForm);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FormAddedSuccessfully,
                paperMetadataResponseDto
            );
        }

        public async Task<ApiResponse> AddManuallySelectedQuestionsAsync(long paperId, AddOrUpdateManualQuestionsRequestDto addManualQuestionsRequestDto, bool usesExcelQuestionsImport = false)
        {
            if (addManualQuestionsRequestDto.SelectedQuestions == null || addManualQuestionsRequestDto.SelectedQuestions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.BadRequest,
                                    Resource.Noquestionsselected);
            }

            if (!usesExcelQuestionsImport)
            {
                var selectedUsedQuestionsCount = addManualQuestionsRequestDto.SelectedQuestions.Where(x => x.PaperQuestionStatus == PaperQuestionStatus.Used).Sum(x => x.SubQuestionsCount);

                if (selectedUsedQuestionsCount != addManualQuestionsRequestDto.QuestionsCount)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.ValidationError,
                                        HttpStatusCode.BadRequest,
                                        Resource.SelectedFormsQuestionsCountDoesNotMatchExpectedQuestionsCount);
                }
            }

            List<ManualPaperItemBankQuestionSection> selectedQuestionList = [];

            foreach (var questionId in addManualQuestionsRequestDto.SelectedQuestions)
            {
                var manualSave = new ManualPaperItemBankQuestionSection
                {
                    ItemBankPointId = questionId.ItemBankPointId,
                    DifficultyLevelId = questionId.DifficultyLevelId,
                    SectionId = questionId.SectionId,
                    QuestionMetaDataId = questionId.QuestionMetadataId
                };

                selectedQuestionList.Add(manualSave);
            }

            _commonService
                  ._unitOfWork
                  .Repository<ManualPaperItemBankQuestionSection, long>()
                  .AddRangAsync(selectedQuestionList);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(paperId, PaperCreationStatus.QuestionsSelectedManually);

                await UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Selectedquestionshavebeenaddedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                Resource.FailedToAddSelectedQuestions);
        }

        public async Task<IApiResponse> AddOrUpdateManualQuestionSectioningAsync(
           long paperId,
           List<ManuallySelectedQuestionsResponseDto> manualQuestions,
           Dictionary<string, List<SectionRequestDto>> formSections,
           Dictionary<string, List<long>> formQuestionMap,
           FormMetadataDto formMetadata
        )
        {
            var targetPaper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (targetPaper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.PaperNotFound);
            }

            foreach (var form in formSections)
            {
                var validationResult = await ValidateManuallyQuestionSectioningAsync(targetPaper, form.Key, form.Value);

                if (!validationResult.IsValid)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        validationResult.Message);
                }
            }

            var response = await _formQuestionScoreService.SaveFormsAndItsQuestions(paperId, formQuestionMap, formMetadata);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.DistributionSavedButQuestionSyncFailed, HttpStatusCode.BadRequest, response.Message, response.Data);
            }

            var currentPaperForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(f => f.PaperId == paperId)
                .AsNoTracking()
                .ToListAsync();

            HashSet<AvailabilityStatus> finalizedStatuses = [AvailabilityStatus.Active, AvailabilityStatus.Synced, AvailabilityStatus.Suspended];

            var existingSections = await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .GetAll(s => s.PaperId == paperId, Including: $"{nameof(StandardSection.ManualQuestions)},{nameof(StandardSection.Form)}")
                .ToListAsync();

            var currentForm = currentPaperForms.FirstOrDefault(f => f.Name.Trim() == formMetadata.Name.Trim());

            if (currentForm == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.FormNotFound);
            }

            var existingSectionsForCurrentForm = existingSections.Where(s => s.FormId == currentForm.Id).ToList();

            var currentIdentifiers = formSections[formMetadata.Name]
                .Where(s => s.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool)
                .Select(s => s.SectioningIdentifier)
                .ToHashSet();

            var sectionsToDelete = existingSectionsForCurrentForm
                .Where(s => s.Form != null && !currentIdentifiers.Contains(s.SectioningIdentifier))
                .ToList();

            if (sectionsToDelete.Count > 0)
            {
                foreach (var existingSection in sectionsToDelete)
                {
                    existingSection.ManualQuestions.Clear();

                    _commonService
                        ._unitOfWork
                        .Repository<StandardSection, long>()
                        .SoftDelete(existingSection);
                }
            }

            var newSections = new List<StandardSection>();

            var existingIdentifiers = existingSectionsForCurrentForm
                .Select(s => s.SectioningIdentifier)
                .ToHashSet();

            var sectionDtoMap = formSections[formMetadata.Name]
                .GroupBy(d => d.SectioningIdentifier)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var existingSection in existingSectionsForCurrentForm)
            {
                if (sectionDtoMap.TryGetValue(existingSection.SectioningIdentifier, out var dto))
                {
                    existingSection.Name = dto.Name;
                    existingSection.IsRestrictedTime = dto.IsRestrictedTime;
                    existingSection.TimeInMinutes = dto.TimeInMinutes;
                    existingSection.IsRandom = dto.IsRandom;
                    existingSection.OrderId = dto.OrderId;
                    existingSection.InstructionSectionTemplateId = dto.InstructionSectionTemplateId;
                }
            }

            foreach (var sectionDto in formSections[formMetadata.Name]
                .Where(s => s.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool && !existingIdentifiers.Contains(s.SectioningIdentifier)))
            {
                newSections.Add(new StandardSection
                {
                    PaperId = paperId,
                    Name = sectionDto.Name,
                    SectioningIdentifier = sectionDto.SectioningIdentifier,
                    IsRestrictedTime = sectionDto.IsRestrictedTime,
                    TimeInMinutes = sectionDto.TimeInMinutes,
                    IsRandom = sectionDto.IsRandom,
                    OrderId = sectionDto.OrderId,
                    InstructionSectionTemplateId = sectionDto.InstructionSectionTemplateId,
                    FormId = currentForm.Id
                });
            }

            if (newSections.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<StandardSection, long>()
                    .AddRangAsync(newSections);
            }

            var sectionsByIdentifier = new Dictionary<string, StandardSection>();

            foreach (var s in newSections)
            {
                sectionsByIdentifier[s.SectioningIdentifier] = s;
            }

            foreach (var existingSection in existingSectionsForCurrentForm)
            {
                if (!sectionsByIdentifier.ContainsKey(existingSection.SectioningIdentifier))
                {
                    sectionsByIdentifier[existingSection.SectioningIdentifier] = existingSection;
                }
            }

            var itemBankPointIds = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(p => p.PaperId == paperId).Select(p => p.Id)
                .ToListAsync();

            var allPaperQuestions = await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAllAsync(q => itemBankPointIds.Contains(q.ItemBankPointId));

            var currentQuestionIds = manualQuestions.Select(q => q.Id).ToHashSet();
            var manualQuestionsById = manualQuestions.ToDictionary(x => x.Id); // Note: Id here is NOT question metadata Id, it's ManualPaperItemBankQuestionSection Id

            foreach (var dbQuestion in allPaperQuestions.Where(q => currentQuestionIds.Contains(q.Id)))
            {
                if (manualQuestionsById.TryGetValue(dbQuestion.Id, out var targetPaperItemBankQuestion) &&
                    targetPaperItemBankQuestion.SectioningIdentifier != null &&
                    sectionsByIdentifier.TryGetValue(targetPaperItemBankQuestion.SectioningIdentifier, out var section))
                {
                    dbQuestion.Section = section;
                }
                else
                {
                    dbQuestion.Section = null;
                }
            }
            await _commonService._unitOfWork.Complete();

            await UpdatePaperCreationStatusAsync(new UpdatePaperCreationStatusRequestDto(paperId, PaperCreationStatus.SectionsAdded));

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.Questionssectionedsuccessfully);
        }

        public async Task<IApiResponse> AddOrUpdateSectionsDistributionsForAutoAsync(AddOrUpdateAutoSectionsDistributionsRequestDto addOrUpdateAutoSectionsDistributionsRequestDto)
        {
            // VALIDATING DATA

            var ValidateQuestionTypeAndDifficultyDistribution = await _questionDistributionValidationService.ValidateQuestionTypeAndDifficultyDistributionAsync(addOrUpdateAutoSectionsDistributionsRequestDto);

            if (ValidateQuestionTypeAndDifficultyDistribution.StatusCode != HttpStatusCode.OK)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    ValidateQuestionTypeAndDifficultyDistribution.Message);
            }

            var validationResult = await ValidateAutoDistributionDataAsync(addOrUpdateAutoSectionsDistributionsRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message);
            }

            // FETCH EXISTING SECTIONS AND ITEM BANK POINTS

            var existingSections = await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .GetAll(s => s.PaperId == addOrUpdateAutoSectionsDistributionsRequestDto.PaperId, Including: nameof(StandardSection.AutoQuestions))
                .ToListAsync();

            var paperItembankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(x => x.PaperId == addOrUpdateAutoSectionsDistributionsRequestDto.PaperId)
                .ToListAsync();

            var newAutoDistributions = new List<AutoPaperItemBankQuestionSection>();

            var newSections = new List<StandardSection>();

            foreach (var sectionDto in addOrUpdateAutoSectionsDistributionsRequestDto.Sections)
            {
                // REUSE EXISTING SECTION IF MATCHED BY OrderId, OTHERWISE CREATE NEW

                var section = existingSections.FirstOrDefault(s => s.OrderId == sectionDto.OrderId);

                if (section is null)
                {
                    section = new StandardSection
                    {
                        PaperId = addOrUpdateAutoSectionsDistributionsRequestDto.PaperId,
                    };

                    newSections.Add(section);
                }

                section.Name = sectionDto.Name;
                section.IsRestrictedTime = sectionDto.IsRestrictedTime;
                section.TimeInMinutes = sectionDto.TimeInMinutes;
                section.IsRandom = sectionDto.IsRandom;
                section.InstructionSectionTemplateId = sectionDto.InstructionSectionTemplateId;
                section.OrderId = sectionDto.OrderId;

                if (section.AutoQuestions?.Count > 0)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<AutoPaperItemBankQuestionSection, long>()
                        .DeleteRange(section.AutoQuestions);
                }

                foreach (var distribution in sectionDto.Distributions)
                {
                    var matchingItemBankPoint = paperItembankPoints.Find(x => x.ItemBankId == distribution.ItemBankId);

                    var newAutoDistribution = _mapper.Map<AutoPaperItemBankQuestionSection>(distribution);

                    newAutoDistribution.ItemBankPointId = matchingItemBankPoint.Id;
                    newAutoDistribution.Section = section;
                    newAutoDistribution.QuestionIds = JsonConvert.SerializeObject(distribution.QuestionIds);

                    newAutoDistributions.Add(newAutoDistribution);
                }
            }

            if (newSections.Count > 0)
            {
                await _commonService
                    ._unitOfWork
                    .Repository<StandardSection, long>()
                    .AddRangeAsync(newSections);
            }

            _commonService._unitOfWork.Repository<AutoPaperItemBankQuestionSection, long>().AddRangAsync(newAutoDistributions);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(addOrUpdateAutoSectionsDistributionsRequestDto.PaperId, PaperCreationStatus.SectionsAdded);

                await UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

                var response = await _formQuestionScoreService.SaveFormsAndItsQuestions(addOrUpdateAutoSectionsDistributionsRequestDto.PaperId, null, null);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.DistributionSavedButQuestionSyncFailed,
                                        HttpStatusCode.OK,
                                        Resource.DistributionSavedButQuestionAndFormsSyncFailed,
                                        response.Data);
                }

                return _commonService._apiResponse.GetApiResponse(
                                                   CustomCodeStatus.Success,
                                                   HttpStatusCode.OK,
                                                   Resource.SectionAutoQuestionsConfigured);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                                                   CustomCodeStatus.SomethingWentWrong,
                                                   HttpStatusCode.InternalServerError,
                                                   Resource.FailedToConfigureSectionAutoQuestions);
            }
        }

        public async Task<IApiResponse> SavePaperMetadataTemplateAsync(PaperMetadataSavingRequestDto PaperTemplateDto)
        {
            var templateNameExists = await _commonService
               ._unitOfWork
               .Repository<PaperMetadataTemplate, long>()
               .IsExistAsync(q => q.Name.ToLower() == PaperTemplateDto.Name.ToLower());

            if (templateNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var _object = JsonSerializer.Serialize(PaperTemplateDto.AddOrUpdatePaperMetadataRequestDto);
            var jObject = JObject.Parse(_object);

            PaperMetadataTemplate template = new()
            {
                Name = PaperTemplateDto.Name,
                Data = jObject.ToString()
            };

            await _commonService
                ._unitOfWork
                .Repository<PaperMetadataTemplate, long>()
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
                                    Resource.OperationFailed);
            }
        }


        // UPDATE METHODS

        public async Task<IApiResponse> UpdatePaperMetadataAsync(long paperId, AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto)
        {
            var validationResult = await ValidatePaperMetadataRequestAsync(editPaperMetadataRequestDto, paperId);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message);
            }

            const string includes = $"{nameof(PaperMetadata.Subjects)},{nameof(PaperMetadata.Language)}," +
                                    $"{nameof(PaperMetadata.PaperStageCategoryDecisionPaths)}," +
                                    $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Stages)}.{nameof(Stage.CategoryDecisionPaths)}";

            var existingPaper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId, includes);

            if (existingPaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            _mapper.Map(editPaperMetadataRequestDto, existingPaper);

            existingPaper.AdaptiveSubtype = existingPaper.Type == PaperType.Adaptive ? editPaperMetadataRequestDto.AdaptiveSubtype : AdaptivePaperSubtype.None;

            existingPaper.IsStepPlus = editPaperMetadataRequestDto.IsStepPlus;

            existingPaper.PaperCreationStatus = PaperCreationStatus.MetadataAdded;

            var groupRepo = _commonService._unitOfWork.Repository<PaperGroups, long>();

            var oldGroups = await groupRepo.GetAllAsync(
                x => x.PaperId == existingPaper.Id,
                Including: "OESGroup");

            var ownerGroup = oldGroups.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser);

            if (ownerGroup == null)
            {
                ownerGroup = await groupRepo.GetObjAsync(
                    g => g.PaperId == existingPaper.Id && g.OESGroup.AutoCreatedForUser,
                    Including: "OESGroup"
                );
            }

            if (editPaperMetadataRequestDto.OESGroupDtos != null)
            {
                foreach (var g in oldGroups)
                {
                    if (ownerGroup != null && g.OESGroupId == ownerGroup.OESGroupId)
                        continue;

                    bool stillExists = editPaperMetadataRequestDto.OESGroupDtos.Any(dto => dto.Id == g.OESGroupId);

                    if (!stillExists)
                    {
                        groupRepo.Delete(g);
                    }
                }
            }

            if (editPaperMetadataRequestDto.OESGroupDtos != null)
            {
                foreach (var g in editPaperMetadataRequestDto.OESGroupDtos.Where(x => !x.AutoCreatedForUser))
                {
                    if (!oldGroups.Any(og => og.OESGroupId == g.Id))
                    {
                        await groupRepo.AddAsync(new PaperGroups
                        {
                            PaperId = existingPaper.Id,
                            OESGroupId = g.Id
                        });
                    }
                }
            }

            if (existingPaper.Type == PaperType.Adaptive && editPaperMetadataRequestDto.CategoryStagePaths != null)
            {
                HandleAdaptivePaperFormUpdate(existingPaper, editPaperMetadataRequestDto);
            }

            if (editPaperMetadataRequestDto.AdaptiveCategoryExecutionOrder?.Count > 0)
            {
                existingPaper.AdaptiveCategoryExecutionOrder = JsonConvert.SerializeObject(editPaperMetadataRequestDto.AdaptiveCategoryExecutionOrder);
            }
            else
            {
                existingPaper.AdaptiveCategoryExecutionOrder = null;
            }

            await _commonService._unitOfWork.Complete();

            var paperMetadataResponseDto = new AddOrUpdatePaperMetadataResponseDto(existingPaper.Id,
                                                                                   existingPaper.Name,
                                                                                   existingPaper.Type,
                                                                                   existingPaper.QuestionSelectionType,
                                                                                   existingPaper.PaperCreationStatus,
                                                                                   existingPaper.QuestionsCount,
                                                                                   existingPaper.Duration,
                                                                                   existingPaper.TotalMarks,
                                                                                   existingPaper.DifficultyProfileId,
                                                                                   existingPaper.LanguageId,
                                                                                   existingPaper.Language.Name,
                                                                                   existingPaper.OutputFormsCount,
                                                                                   existingPaper.AllowInstantResult,
                                                                                   existingPaper.UsesExcelQuestionsImport,
                                                                                   existingPaper.StageCount,
                                                                                   existingPaper.QuestionDistributionTypeInForm,
                                                                                   existingPaper.Type == PaperType.Adaptive ? existingPaper.DPathCalculationMode : DPathCalculationMode.ManualFinalScore,
                                                                                   existingPaper.AdaptiveSubtype,
                                                                                   existingPaper.IsStepPlus);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperUpdatedSuccessfully,
                paperMetadataResponseDto
            );
        }

        public async Task<IApiResponse> UpdatePaperCreationStatusAsync(UpdatePaperCreationStatusRequestDto updatePaperCreationStatusRequestDto)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == updatePaperCreationStatusRequestDto.PaperId);

            var lastForm = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .Query()
                .Where(f => f.PaperId == updatePaperCreationStatusRequestDto.PaperId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (lastForm != null)
            {
                if (updatePaperCreationStatusRequestDto.PaperCreationStatus == PaperCreationStatus.PaperCreated)
                {
                    if (lastForm.FormStatus == AvailabilityStatus.InComplete)
                    {
                        lastForm.FormStatus = AvailabilityStatus.Active;
                    }
                }
                else if (updatePaperCreationStatusRequestDto.PaperCreationStatus == PaperCreationStatus.SectionsAdded &&
                         lastForm.FormStatus != AvailabilityStatus.Active &&
                         lastForm.FormStatus != AvailabilityStatus.Synced &&
                         lastForm.FormStatus != AvailabilityStatus.Suspended)
                {
                    lastForm.FormStatus = AvailabilityStatus.InComplete;
                }
            }

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            paper.PaperCreationStatus = updatePaperCreationStatusRequestDto.PaperCreationStatus;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperCreationStatusUpdated
            );
        }

        public async Task<IApiResponse> UpdateManuallySelectedQuestionsAsync(long paperId, long? formId, AddOrUpdateManualQuestionsRequestDto updateManualQuestionsRequestDto)
        {
            if (updateManualQuestionsRequestDto.SelectedQuestions == null || updateManualQuestionsRequestDto.SelectedQuestions.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.BadRequest,
                                    Resource.NoQuestionsSelectedToUpdate);
            }

            var selectedUsedQuestionsCount = updateManualQuestionsRequestDto.SelectedQuestions.Where(x => x.PaperQuestionStatus == PaperQuestionStatus.Used).Sum(x => x.SubQuestionsCount);

            if (selectedUsedQuestionsCount != updateManualQuestionsRequestDto.QuestionsCount)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.ValidationError,
                                    HttpStatusCode.BadRequest,
                                    Resource.NumberOfQuestions);
            }

            var existingQuestions = await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAll(q => q.ItemBankPoint.PaperId == paperId && (q.SectionId == null || q.Section.FormId == formId),
                        Including: $"{nameof(ManualPaperItemBankQuestionSection.ItemBankPoint)}," + $"{nameof(ManualPaperItemBankQuestionSection.Section)}")
                .ToListAsync();

            var existingIds = existingQuestions.Select(q => q.Id).ToHashSet();
            var newQuestionsDtosToBeAdded = updateManualQuestionsRequestDto.SelectedQuestions.Where(q => q.PaperItemBankQuestionId == 0 || !existingIds.Contains(q.PaperItemBankQuestionId)).ToList(); // NOTE: Don't use ExceptBy here as it de-duplicates resulted list based on the specified key selector.

            var existingQuestionsToBeDeleted = existingQuestions.ExceptBy(updateManualQuestionsRequestDto.SelectedQuestions.Select(q => q.PaperItemBankQuestionId), q => q.Id).ToList();

            if (newQuestionsDtosToBeAdded.Count > 0)
            {
                var newQuestionsToBeAdded = newQuestionsDtosToBeAdded.ConvertAll(x => new ManualPaperItemBankQuestionSection
                {
                    QuestionMetaDataId = x.QuestionMetadataId,
                    ItemBankPointId = x.ItemBankPointId,
                    DifficultyLevelId = x.DifficultyLevelId,
                    SectionId = x.SectionId,
                    PaperQuestionStatus = x.PaperQuestionStatus
                });

                _commonService
                    ._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .AddRangAsync(newQuestionsToBeAdded);
            }

            if (existingQuestionsToBeDeleted.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .DeleteRange(existingQuestionsToBeDeleted);
            }

            await _commonService._unitOfWork.Complete();

            var existingQuestionsToBeDeletedSectionsIds = existingQuestionsToBeDeleted.ConvertAll(q => q.SectionId);

            await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .Query()
                .Where(x => existingQuestionsToBeDeletedSectionsIds.Contains(x.Id))
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.DeletedDate, DateTimeHelper.Now)
                );

            var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(paperId, PaperCreationStatus.QuestionsSelectedManually);

            await UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Selectedquestionsupdatedsuccessfully
            );
        }

        // DELETE METHODS

        public async Task<ApiResponse> DeletePaperMetadataTemplateAsync(long templateId)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<PaperMetadataTemplate, long>()
                .GetObjAsync(x => x.Id == templateId);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound
                );
            }

            _commonService._unitOfWork.Repository<PaperMetadataTemplate, long>().Delete(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.TemplateDeletedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToDeleteTemplate
            );
        }

        public async Task<IApiResponse> DeletePaperAsync(long id)
        {
            var checkResult = await IsPaperInScheduleAsync(id);

            if (checkResult)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.PaperAssignedToScheduleANDCannotBeDeleted);
            }

            var includes = string.Join(",", nameof(PaperMetadata.PaperStageCategoryDecisionPaths),
                                            nameof(PaperMetadata.ItemBanksPoints),
                                            $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Sections)}",
                                            $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.Stages)}.{nameof(Stage.AdaptiveSections)}.{nameof(AdaptiveSection.PaperBlocks)}");

            var paperRepo = _commonService._unitOfWork.Repository<PaperMetadata, long>();

            var paper = await paperRepo.GetObjAsync(p => p.Id == id, Including: includes);

            if (paper == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.PaperNotFound);
            }

            foreach (var path in paper.PaperStageCategoryDecisionPaths ?? [])
                _commonService._unitOfWork.Repository<PaperStageCategoryDecisionPath, long>().SoftDelete(path);

            foreach (var point in paper.ItemBanksPoints ?? [])
                _commonService._unitOfWork.Repository<PaperItemBankPoint, long>().SoftDelete(point);

            foreach (var form in paper.Forms ?? [])
            {
                foreach (var section in form.Sections ?? [])
                    _commonService._unitOfWork.Repository<StandardSection, long>().SoftDelete(section);

                foreach (var stage in form.Stages ?? [])
                {
                    foreach (var adaptiveSection in stage.AdaptiveSections ?? [])
                    {
                        foreach (var block in adaptiveSection.PaperBlocks ?? [])
                            _commonService._unitOfWork.Repository<PaperFormBlock, long>().SoftDelete(block);

                        _commonService._unitOfWork.Repository<AdaptiveSection, long>().SoftDelete(adaptiveSection);
                    }

                    _commonService._unitOfWork.Repository<Stage, long>().SoftDelete(stage);
                }

                _commonService._unitOfWork.Repository<PaperForm, long>().SoftDelete(form);
            }

            paperRepo.SoftDelete(paper);

            var result = await _commonService._unitOfWork.Complete();

            return result > 0
                ? _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.PaperDeletedSuccessfully)
                : _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, Resource.FailedToDeletePaper);
        }

        // LOCK METHODS

        public async Task<ApiResponse> AcquirePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            if (string.IsNullOrWhiteSpace(paperLockRequestDto.SessionId))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InvalidParameter,
                    HttpStatusCode.BadRequest,
                    Resource.SessionRequired
                );
            }

            var now = DateTimeHelper.Now;
            var expiresAt = now.AddSeconds(60);

            var updatedRows = await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(p => p.Id == paperLockRequestDto.PaperId &&
                             (p.LockSessionId == null ||
                              p.LockExpiresAt == null ||
                              p.LockExpiresAt <= now ||
                              p.LockSessionId == paperLockRequestDto.SessionId))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.LockSessionId, paperLockRequestDto.SessionId)
                    .SetProperty(p => p.LockExpiresAt, expiresAt));

            if (updatedRows == 1)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new AcquirePaperLockResponseDto(paperLockRequestDto.SessionId, expiresAt)
                );
            }

            var paperExists = await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .IsExistAsync(p => p.Id == paperLockRequestDto.PaperId);

            return _commonService._apiResponse.GetApiResponse(
                paperExists ? CustomCodeStatus.Conflict : CustomCodeStatus.NotFound,
                paperExists ? HttpStatusCode.Conflict : HttpStatusCode.NotFound,
                paperExists ? Resource.LockedByAnotherUser : Resource.PaperNotFound
            );
        }

        public async Task<ApiResponse> RenewPaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            if (string.IsNullOrWhiteSpace(paperLockRequestDto.SessionId))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InvalidParameter,
                    HttpStatusCode.BadRequest,
                    Resource.SessionRequired
                );
            }

            var now = DateTimeHelper.Now;
            var expiresAt = now.AddSeconds(60);

            var updatedRows = await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(p => p.Id == paperLockRequestDto.PaperId &&
                             p.LockSessionId == paperLockRequestDto.SessionId &&
                             p.LockExpiresAt != null &&
                             p.LockExpiresAt > now)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.LockExpiresAt, expiresAt));

            if (updatedRows == 1)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new AcquirePaperLockResponseDto(paperLockRequestDto.SessionId, expiresAt)
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Conflict,
                HttpStatusCode.Conflict,
                Resource.LockedByAnotherUser
            );
        }

        public async Task<ApiResponse> ReleasePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            if (string.IsNullOrWhiteSpace(paperLockRequestDto.SessionId))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InvalidParameter,
                    HttpStatusCode.BadRequest,
                    Resource.SessionRequired
                );
            }

            await _commonService._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(p => p.Id == paperLockRequestDto.PaperId && p.LockSessionId == paperLockRequestDto.SessionId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.LockSessionId, (string)null)
                    .SetProperty(p => p.LockExpiresAt, (DateTime?)null));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK
            );
        }

        #region Helper Methods

        private async Task<(bool IsValid, string Message)> HandleManualPaperFormAdditionUsingExcelFileAsync(PaperMetadata paperMetadata, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto)
        {
            // 0) Validating request before proceeding

            if (paperMetadata == null)
            {
                return (false, Resource.PaperNotFound);
            }

            if (addOrUpdatePaperMetadataRequestDto?.UploadedQuestions?.ItemBankIds == null ||
                addOrUpdatePaperMetadataRequestDto.UploadedQuestions.ItemBankIds.Count == 0 ||
                addOrUpdatePaperMetadataRequestDto.UploadedQuestions.QuestionIds == null ||
                addOrUpdatePaperMetadataRequestDto.UploadedQuestions.QuestionIds.Count == 0
            )
            {
                return (false, Resource.NoQuestionsSelectedToUpdate);
            }

            if (addOrUpdatePaperMetadataRequestDto.UploadedQuestions.ValidateExcelSheetQuestionsDto?.QuestionWithSectionNameDtos == null ||
                addOrUpdatePaperMetadataRequestDto.UploadedQuestions.ValidateExcelSheetQuestionsDto.QuestionWithSectionNameDtos.Count == 0
            )
            {
                return (false, Resource.OperationFailed);
            }

            // 1) Handling manual paper stepper, step 2, item banks selection:

            var paperDistinctItemBanksIds = new HashSet<long>(addOrUpdatePaperMetadataRequestDto.UploadedQuestions.ItemBankIds);

            var paperItemBankPoints = (await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAllAsync(pip => pip.PaperId == paperMetadata.Id, asNoTracking: true))
                .ToList();

            foreach (var point in paperItemBankPoints)
            {
                paperDistinctItemBanksIds.Add(point.ItemBankId);
            }

            var itemBankIdsToBindToPaper = paperDistinctItemBanksIds.ExceptBy(paperItemBankPoints.Select(it => it.ItemBankId), id => id).ToList();

            if (itemBankIdsToBindToPaper.Count > 0)
            {
                var addItemBankPointDto = new AddOrUpdateItemBankPointRequestDto(
                    paperMetadata.Id,
                    paperMetadata.QuestionSelectionType,
                    [.. paperDistinctItemBanksIds],
                    paperMetadata.LanguageId,
                    paperMetadata.DifficultyProfileId,
                    paperMetadata.QuestionsCount,
                    paperMetadata.OutputFormsCount,
                    paperMetadata.AllowInstantResult,
                    paperMetadata.QuestionDistributionTypeInForm
                );

                var itemBankPointService = _serviceProvider.GetRequiredService<IItemBankPointService>();

                var itemBankPointResponse = await itemBankPointService.AddItemBankPointAsync(addItemBankPointDto);

                if (itemBankPointResponse.StatusCode != HttpStatusCode.OK)
                    return (false, itemBankPointResponse.Message);

                if (itemBankPointResponse.Data is List<PaperItemBankPointDto> newPoints)
                {
                    paperItemBankPoints.AddRange(newPoints.Select(p => new PaperItemBankPoint
                    {
                        Id = p.Id,
                        PaperId = p.PaperId,
                        ItemBankId = p.ItemBankId
                    }));
                }
            }

            var itemBankPointDict = paperItemBankPoints.ToDictionary(
                ibp => ibp.ItemBankId,
                ibp => ibp.Id
            );


            // 2) Handling manual paper stepper, extra step, adding single form:

            var paperFormEntity = new PaperForm
            {
                PaperId = paperMetadata.Id,
                Name = $"Excel Manual Form {paperMetadata.OutputFormsCount}",
                Code = $"Excel Manual Form Code {paperMetadata.OutputFormsCount}",
                Description = $"Excel Manual Form Description {paperMetadata.OutputFormsCount}",
                FormStatus = AvailabilityStatus.Active
            };


            // 3) Handling manual paper stepper, step 4, sections:

            var questionsWithSectionNames = addOrUpdatePaperMetadataRequestDto.UploadedQuestions.ValidateExcelSheetQuestionsDto.QuestionWithSectionNameDtos;

            var distinctSections = questionsWithSectionNames
                .Select(q => q.SectionName)
                .Where(sn => !string.IsNullOrWhiteSpace(sn))
                .Distinct()
                .ToList();

            var sections = new List<StandardSection>();

            foreach (var sectionName in distinctSections)
            {
                var section = new StandardSection
                {
                    PaperId = paperMetadata.Id,
                    Name = sectionName,
                    SectioningIdentifier = RandomIntegerGenerator.GenerateShortUniqueNumber().ToString(),
                    IsRestrictedTime = false,
                    TimeInMinutes = 0,
                    OrderId = distinctSections.IndexOf(sectionName) + 1,
                };

                sections.Add(section);
            }

            paperFormEntity.Sections = sections;

            await _commonService._unitOfWork.Repository<PaperForm, long>().AddAsync(paperFormEntity);

            await _commonService._unitOfWork.Complete();


            // 4) Handling manual paper stepper, step 3, questions selection:

            var sectionDict = sections.ToDictionary(s => s.Name.Trim().ToLower(), s => s.Id);

            var questionsWithItemBanks = await GetQuestionsWithItemBanksAsync(addOrUpdatePaperMetadataRequestDto.UploadedQuestions);

            var questionIds = questionsWithItemBanks.ConvertAll(q => q.QuestionId);

            var questionMetadatas = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(qm => questionIds.Contains(qm.Id) && qm.CurrentExhaustionCount < qm.QuestionsExhaustionCount, Including: nameof(QuestionMetadata.SubQuestions));

            var orderedQuestionMetadatas = questionMetadatas
                .OrderBy(qm => questionIds.IndexOf(qm.Id))
                .ToList();

            var questionMetaDict = orderedQuestionMetadatas.ToDictionary(
                qm => qm.Id,
                qm => (DifficultyLevelId: qm.DifficultyLevelId, SubQuestionsCount: qm.SubQuestions.Count == 0 ? 1 : qm.SubQuestions.Count)
            );

            var sectionNameDict = questionsWithSectionNames
                .Where(v => !string.IsNullOrWhiteSpace(v.SectionName))
                .GroupBy(v => v.QuestionId)
                .ToDictionary(g => g.Key, g => g.First().SectionName.Trim().ToLower());

            var questionSaveDtos = questionsWithItemBanks
                .Where(q => questionMetaDict.ContainsKey(q.QuestionId) && itemBankPointDict.ContainsKey(q.ItemBankId))
                .Select(q =>
                {
                    long? sectionId = null;

                    if (sectionNameDict.TryGetValue(q.QuestionId, out var secName) && sectionDict.TryGetValue(secName, out var secId))
                    {
                        sectionId = secId;
                    }

                    return new QuestionSaveDto(
                        0,
                        QuestionMetadataId: q.QuestionId,
                        ItemBankPointId: itemBankPointDict[q.ItemBankId],
                        DifficultyLevelId: questionMetaDict[q.QuestionId].DifficultyLevelId,
                        SectionId: sectionId,
                        PaperQuestionStatus: PaperQuestionStatus.Used,
                        SubQuestionsCount: questionMetaDict[q.QuestionId].SubQuestionsCount
                    );
                })
                .ToList();

            var manualQuestionsDto = new AddOrUpdateManualQuestionsRequestDto(
                questionSaveDtos,
                paperMetadata.QuestionsCount
            );

            var manualQuestionsResponse = await AddManuallySelectedQuestionsAsync(paperMetadata.Id, manualQuestionsDto, true);

            if (manualQuestionsResponse.StatusCode != HttpStatusCode.OK)
                return (false, manualQuestionsResponse.Message);

            // 5) Handling manual paper stepper, extra step, adding single form:

            var formQuestionsEntities = questionSaveDtos.ConvertAll(q => new GeneratedFormQuestion
            {
                FormId = paperFormEntity.Id,
                QuestionId = q.QuestionMetadataId,
                FormQuestionStatus = PaperQuestionStatus.Used,
                Score = 0
            });

            await _commonService._unitOfWork.Repository<GeneratedFormQuestion, long>().AddRangeAsync(formQuestionsEntities);

            await _commonService._unitOfWork.Complete();

            return (true, string.Empty);
        }

        private static void HandleAdaptivePaperFormAddition(PaperMetadata paperMetadata, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto)
        {
            var adaptivePaperSubType = addOrUpdatePaperMetadataRequestDto.AdaptiveSubtype;
            bool isStep = adaptivePaperSubType == AdaptivePaperSubtype.STEP;
            bool isStepplus = addOrUpdatePaperMetadataRequestDto.IsStepPlus;
            const int stagesPerGroup = 3;

            var defaultForm = new PaperForm
            {
                Name = adaptivePaperSubType == AdaptivePaperSubtype.MST ? "Default Adaptive Form - MST" : isStepplus ? "Default Adaptive Form - STEPPlus" : "Default Adaptive Form - STEP",
                Code = "ADF-001",
                Description = "This is the default adaptive form.",
                FormStatus = AvailabilityStatus.Active,
                Stages = []
            };

            for (int i = 0; i < paperMetadata.StageCount; i++)
            {
                string renderedPartName = isStep
                    ? GetGroupSectionName(i / stagesPerGroup)
                    : i == 0 ? Resource.StageFormat : null;

                var stage = new Stage
                {
                    Name = $"{Resource.Stage} {i + 1}",
                    RenderedPartName = renderedPartName,
                    Order = i + 1,
                    TimeInMinutes = 0,
                    InstructionSectionTemplateId = null,
                    CategoryDecisionPaths = []
                };

                if (addOrUpdatePaperMetadataRequestDto.DPathCalculationMode == DPathCalculationMode.FullyManual &&
                    addOrUpdatePaperMetadataRequestDto.CategoryStagePaths?.Count > 0
                )
                {
                    foreach (var categoryEntry in addOrUpdatePaperMetadataRequestDto.CategoryStagePaths)
                    {
                        long categoryId = categoryEntry.Key;

                        List<decimal> paths = categoryEntry.Value;

                        if (i < paths.Count)
                        {
                            stage.CategoryDecisionPaths.Add(new PaperStageCategoryDecisionPath
                            {
                                QuestionCategoryId = categoryId,
                                DecisionPathValue = paths[i],
                                FixedDPath = null,
                                Paper = paperMetadata
                            });
                        }
                    }
                }

                defaultForm.Stages.Add(stage);
            }

            if (addOrUpdatePaperMetadataRequestDto.DPathCalculationMode == DPathCalculationMode.ManualFinalScore &&
                addOrUpdatePaperMetadataRequestDto.CategoryFixedDPaths?.Count > 0
            )
            {
                paperMetadata.PaperStageCategoryDecisionPaths ??= [];

                foreach (var categoryEntry in addOrUpdatePaperMetadataRequestDto.CategoryFixedDPaths)
                {
                    paperMetadata.PaperStageCategoryDecisionPaths.Add(new PaperStageCategoryDecisionPath
                    {
                        QuestionCategoryId = categoryEntry.Key,
                        DecisionPathValue = null,
                        FixedDPath = categoryEntry.Value,
                        StageId = null,
                        Paper = paperMetadata
                    });
                }
            }

            paperMetadata.Forms = [defaultForm];
        }

        private static void HandleAdaptivePaperFormUpdate(
            PaperMetadata existingPaper,
            AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto)
        {
            var defaultForm = existingPaper.Forms.FirstOrDefault();

            if (defaultForm?.Stages == null) return;

            var mode = editPaperMetadataRequestDto.DPathCalculationMode;

            if (mode == DPathCalculationMode.FullyAutomatic) return;

            if (mode == DPathCalculationMode.FullyManual)
            {
                var orderedStages = defaultForm.Stages.OrderBy(s => s.Order).ToList();

                foreach (var categoryEntry in editPaperMetadataRequestDto.CategoryStagePaths)
                {
                    long categoryId = categoryEntry.Key;

                    List<decimal> incomingPathValues = categoryEntry.Value;

                    for (int i = 0; i < orderedStages.Count; i++)
                    {
                        if (i < incomingPathValues.Count)
                        {
                            var currentStage = orderedStages[i];

                            var existingPathEntity = currentStage
                                .CategoryDecisionPaths
                                .FirstOrDefault(x => x.QuestionCategoryId == categoryId);

                            if (existingPathEntity != null)
                            {
                                existingPathEntity.DecisionPathValue = incomingPathValues[i];
                            }
                        }
                    }
                }
            }

            if (mode == DPathCalculationMode.ManualFinalScore)
            {
                var categoryLevelPaths = existingPaper.PaperStageCategoryDecisionPaths?
                    .Where(p => !p.StageId.HasValue)
                    .ToList() ?? [];

                if (editPaperMetadataRequestDto.CategoryFixedDPaths != null)
                {
                    foreach (var categoryEntry in editPaperMetadataRequestDto.CategoryFixedDPaths)
                    {
                        long categoryId = categoryEntry.Key;

                        decimal fixedDPathValue = categoryEntry.Value;

                        var existingCategoryPath = categoryLevelPaths
                            .FirstOrDefault(x => x.QuestionCategoryId == categoryId);

                        if (existingCategoryPath != null)
                        {
                            existingCategoryPath.FixedDPath = fixedDPathValue;
                        }
                    }
                }
            }
        }

        private async Task<bool> IsPaperInScheduleAsync(long paperId)
        {
            var paperInSchedule = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .IsExistAsync(p => p.PaperId == paperId);

            return paperInSchedule;
        }

        private async Task<(bool IsValid, string Message)> ValidateAutoDistributionDataAsync(AddOrUpdateAutoSectionsDistributionsRequestDto requestDto)
        {
            var targetPaper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(result => result.Id == requestDto.PaperId);
            if (targetPaper == null)
            {
                return (false, Resource.Specifiedpaperdoesnotexist);
            }

            var allSections = requestDto.Sections.ToList();
            var duplicateOrderIds = allSections
                .GroupBy(s => s.OrderId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateOrderIds.Count > 0)
            {
                var duplicateSections = allSections
                    .Where(s => duplicateOrderIds.Contains(s.OrderId))
                    .Select(s => $"{s.Name} (Order {s.OrderId})");

                return (false, string.Format(Resource.DuplicateSectionsOrderNumbersDetected, string.Join(", ", duplicateSections)));
            }

            var questionsCountsMatch = targetPaper.QuestionsCount == requestDto.Sections.Sum((SectionWithDistributionsDto x) => x.Distributions.Sum((SectionDistributionDto y) => (y.SelectedCount * y.SubQuestionCount)));
            if (!questionsCountsMatch)
            {
                return (false, Resource.DistributedQuestionsCountMismatch);
            }

            var emptySectionExists = requestDto.Sections.Exists(x => x.Distributions.Count == 0);
            if (emptySectionExists)
            {
                return (false, Resource.SectionsCannotBeEmpty);
            }

            var validationResult = ValidateAutoSectionsDurationsAgainstPaperDuration(requestDto.Sections, targetPaper.Duration);
            if (!validationResult.IsValid)
            {
                return (false, validationResult.ErrorMessage);
            }

            return (true, string.Empty);
        }

        private async Task<(bool IsValid, string Message)> ValidateManuallyQuestionSectioningAsync(
            PaperMetadata targetPaper,
            string formName,
            List<SectionRequestDto> formSections
        )
        {
            var allSectionsWithoutPoolSection = formSections
                .Where(s => !string.Equals(s.SectioningIdentifier, MiscConstants.ManualQuestionsSelectionPool, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var duplicateOrderIds = allSectionsWithoutPoolSection
                .GroupBy(s => s.OrderId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateOrderIds.Count > 0)
            {
                var duplicateSections = allSectionsWithoutPoolSection
                    .Where(s => duplicateOrderIds.Contains(s.OrderId))
                    .Select(s => $"{s.Name} (Order {s.OrderId})");

                return (false, string.Format(Resource.DuplicateSectionsOrderNumbersDetected, string.Join(", ", duplicateSections)));
            }

            var isSectionsCountValid = formSections.Count >= 2;
            if (!isSectionsCountValid)
            {
                return (false, Resource.atleastonesection);
            }

            var validationResult = ValidateManualSectionsDurationsAgainstPaperDuration(formName, formSections, targetPaper.Duration);
            if (!validationResult.IsValid)
            {
                return (false, validationResult.ErrorMessage);
            }

            return (true, string.Empty);
        }

        private static (bool IsValid, string ErrorMessage) ValidateManualSectionsDurationsAgainstPaperDuration(string formName, List<SectionRequestDto> sections, float paperDuration)
        {
            var allSectionsWithoutPool = sections.ExceptBy([MiscConstants.ManualQuestionsSelectionPool], x => x.SectioningIdentifier).ToList();

            var invalidTimedSection = allSectionsWithoutPool.FirstOrDefault(s => s.IsRestrictedTime && s.TimeInMinutes <= 0);
            if (invalidTimedSection != null)
            {
                return (false, string.Format(Resource.SectionRestrictedTimeInvalidDuration, invalidTimedSection.Name));
            }

            const float epsilon = 0.0001f;
            double sum = allSectionsWithoutPool.Where(s => s.IsRestrictedTime).Sum(x => x.TimeInMinutes);

            if (allSectionsWithoutPool.Count > 0 && allSectionsWithoutPool.TrueForAll(x => x.IsRestrictedTime))
            {
                var isValid = Math.Abs(sum - paperDuration) < epsilon;
                var message = isValid ? "" : string.Format(Resource.TimedSectionsDurationMismatch, formName, sum, paperDuration);
                return (isValid, message);
            }
            else if (allSectionsWithoutPool.TrueForAll(x => !x.IsRestrictedTime))
            {
                return (true, "");
            }
            else
            {
                return (false, Resource.SectionsTimingError);
            }
        }

        private static (bool IsValid, string ErrorMessage) ValidateAutoSectionsDurationsAgainstPaperDuration(List<SectionWithDistributionsDto> sections, float paperDuration)
        {
            var invalidTimedSection = sections.FirstOrDefault(s => s.IsRestrictedTime && s.TimeInMinutes <= 0);

            if (invalidTimedSection != null)
            {
                return (false, string.Format(Resource.SectionRestrictedTimeInvalidDuration, invalidTimedSection.Name));
            }

            const float epsilon = 0.0001f;
            double sectionsDurationsSum = sections.Sum(x => x.TimeInMinutes);

            if (sections.TrueForAll(x => x.IsRestrictedTime))
            {
                var isValid = Math.Abs(sectionsDurationsSum - paperDuration) < epsilon;
                return (isValid, string.Format(Resource.RestrictedSectionsDurationMismatch, sectionsDurationsSum, paperDuration));
            }
            else if (sections.TrueForAll(x => !x.IsRestrictedTime))
            {
                return (true, Resource.SectionsAddedSuccessfully);
            }
            else
            {
                return (false, Resource.SectionsTimingError);
            }
        }

        private async Task<(bool IsValid, string Message)> ValidatePaperMetadataRequestAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataDto, long? paperId = null)
        {
            var errors = new List<string>();

            var paperQuery = _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .Query()
                .Where(p => p.Code == paperMetadataDto.Code || p.Name == paperMetadataDto.Name);

            if (paperId != null && paperId.HasValue)
            {
                paperQuery = paperQuery.Where(p => p.Id != paperId);
            }

            var otherSimilarPaper = await paperQuery.FirstOrDefaultAsync();

            if (otherSimilarPaper != null)
            {
                if (otherSimilarPaper.Code == paperMetadataDto.Code)
                    errors.Add(Resource.PaperWithThisCodeAlreadyExists);

                if (otherSimilarPaper.Name == paperMetadataDto.Name)
                    errors.Add(Resource.PaperWithThisNameAlreadyExists);
            }

            if (string.IsNullOrWhiteSpace(paperMetadataDto.Name))
                errors.Add(Resource.PaperNameCannotBeEmpty);

            if (string.IsNullOrWhiteSpace(paperMetadataDto.Code))
                errors.Add(Resource.PaperCodeCannotBeEmpty);

            if (paperMetadataDto.SubjectsIds.Count == 0)
                errors.Add(Resource.SubjectMustBeSelected);

            bool isPaperStandard = paperMetadataDto.Type == PaperType.Standard;

            if (paperMetadataDto.QuestionsCount <= 0)
                errors.Add(Resource.QuestionsCountMustBeGreaterThanZero);

            if (paperMetadataDto.Duration <= 0)
                errors.Add(Resource.DurationMustBeGreaterThanZero);

            if (isPaperStandard && paperMetadataDto.TotalMarks <= 0)
                errors.Add(Resource.TotalMarksMustBeGreaterThanZero);

            if (!Enum.IsDefined(typeof(PaperType), paperMetadataDto.Type))
                errors.Add(Resource.ValidPaperTypeMustBeSelected);

            bool isPaperAdaptive = paperMetadataDto.Type == PaperType.Adaptive;

            if (isPaperAdaptive)
            {
                if (paperMetadataDto.StageCount <= 0)
                    errors.Add(Resource.StagesCountMustBeGreaterThanZero);

                if (paperMetadataDto.DPathCalculationMode == DPathCalculationMode.FullyManual)
                {
                    bool hasInvalidPath = paperMetadataDto.CategoryStagePaths.Values.Any(paths => paths.Count != paperMetadataDto.StageCount || paths.Any(p => p <= 0));

                    if (hasInvalidPath)
                        errors.Add(Resource.AllStagePathsMustBeGreaterThanZero);
                }

                if (paperMetadataDto.DPathCalculationMode == DPathCalculationMode.ManualFinalScore)
                {
                    if (paperMetadataDto.CategoryFixedDPaths == null || paperMetadataDto.CategoryFixedDPaths.Count == 0)
                        errors.Add(Resource.RequiredFieldsMustBeFilled);
                    else if (paperMetadataDto.CategoryFixedDPaths.Values.Any(v => v <= 0))
                        errors.Add(Resource.AllStagePathsMustBeGreaterThanZero);
                }
            }

            if (isPaperAdaptive && (!paperMetadataDto.TransitionProfileId.HasValue || paperMetadataDto.TransitionProfileId <= 0))
                errors.Add(Resource.TransitionProfileRequiredForAdaptivePapers);

            if (isPaperStandard && !Enum.IsDefined(typeof(QuestionSelectionType), paperMetadataDto.QuestionSelectionType))
                errors.Add(Resource.ValidQuestionSelectionTypeRequired);

            if (isPaperStandard && paperMetadataDto.OutputFormsCount <= 0)
                errors.Add(Resource.OutputPaperFormsCountMustBeGreaterThanZero);

            if (paperMetadataDto.LanguageId <= 0)
                errors.Add(Resource.LanguageMustBeSelected);

            var isPaperAuto = paperMetadataDto.QuestionSelectionType == QuestionSelectionType.Auto;

            if (isPaperAuto && !Enum.IsDefined(typeof(QuestionDistributionTypeInForm), paperMetadataDto.QuestionDistributionTypeInForm))
                errors.Add(Resource.ValidQuestionDistributionTypeRequired);

            if (isPaperStandard && paperMetadataDto.DifficultyProfileId <= 0)
                errors.Add(Resource.DifficultyProfileMustBeSelected);

            if (errors.Count > 0)
            {
                var concatenatedErrorsMessage = string.Join($" {Resource.And} ", errors);

                return (false, concatenatedErrorsMessage);
            }

            return (true, string.Empty);
        }

        private async Task<List<QuestionWithItemBankDto>> GetQuestionsWithItemBanksAsync(QuestionIdsAndItemBankIdsDto questionIdsAndItemBankIdsDto)
        {
            var questions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(q => questionIdsAndItemBankIdsDto.QuestionIds.Contains(q.Id));

            var questionLookup = questions.ToDictionary(q => q.Id);

            var result = questionIdsAndItemBankIdsDto
                .QuestionIds
                .Where(id => questionLookup.ContainsKey(id))
                .Select(id =>
                {
                    var q = questionLookup[id];

                    return new QuestionWithItemBankDto
                    {
                        QuestionId = q.Id,
                        ItemBankId = q.ItemBankId
                    };
                })
                .ToList();

            return result;
        }

        private static string GetGroupSectionName(int groupIndex)
        {
            string[] ordinalNames =
            [
                Resource.First,
                Resource.Second,
                Resource.Third,
                Resource.Fourth,
                Resource.Fifth
            ];

            return groupIndex < ordinalNames.Length
                ? string.Format(Resource.SectionNameFormat, ordinalNames[groupIndex])
                : $"{Resource.Section} {groupIndex + 1}";
        }

        public async Task<IApiResponse> GetAvailableManualQuestionsForPaperAsync(
            long paperId,
            PaginationSearchModel paginationSearchModel
        )
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound);
            }

            IQueryable<QuestionMetadata> query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking();

            query = query
                .Include(q => q.QuestionDetails)
                .Include(q => q.QuestionType)
                .Include(q => q.DifficultyLevel)
                .Include(q => q.ItemBank)
                .Include(q => q.SubQuestions)
                .Include(q => q.FormQuestions)
                    .ThenInclude(fq => fq.Form);

            query = query.Where(q =>
                q.QuestionStatus == QuestionStatus.Approved &&
                q.IsRoot &&
                q.ParentId == null &&
                q.CurrentExhaustionCount < q.QuestionsExhaustionCount &&
                q.ItemBank.ItemBankPoints.Any(p => p.PaperId == paperId) &&
                q.DifficultyProfileId == paper.DifficultyProfileId &&
                q.QuestionDetails.Any(qd => qd.LanguageId == paper.LanguageId) &&
                (!paper.AllowInstantResult || q.QuestionType.IsAutoCorrectable));

            if (paginationSearchModel.FilterObj is JsonElement json)
            {
                var filter = json.Deserialize<QuestionFilterPaginationModel>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
                            query = query.Where(q =>
                                q.ItemBank.ItemBankSignature == filter._selectedItemBankSignature);
                        }
                    }
                    else if (filter._selectedItemBank > 0)
                    {
                        query = query.Where(q => q.ItemBankId == filter._selectedItemBank);
                    }

                    if (filter._selectedType > 0)
                        query = query.Where(q => q.QuestionTypeId == filter._selectedType);

                    if (filter._selectedDifficultyLevel > 0)
                        query = query.Where(q => q.DifficultyLevelId == filter._selectedDifficultyLevel);

                    if (filter._fromDelta != 0 || filter._toDelta != 0)
                        query = query.Where(q => q.Delta >= filter._fromDelta && q.Delta <= filter._toDelta);
                }
            }

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
            {
                var key = paginationSearchModel.SearchKey.ToLower();
                query = query.Where(q =>
                    q.Code.ToLower().Contains(key) ||
                    q.QuestionDetails.Any(d => d.Body.ToLower().Contains(key)));
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(q => q.CreationDate).ThenByDescending(q => q.Id)
                : query.OrderBy(q => q.CreationDate).ThenBy(q => q.Id);

            var total = await query.CountAsync();

            var questions = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            // Load ItemBankPoints separately for efficiency
            var questionIds = questions.ConvertAll(q => q.Id);
            var itemBankIds = questions.Select(q => q.ItemBankId).Distinct().ToList();

            var itemBankPointsLookup = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(ibp => ibp.PaperId == paperId && itemBankIds.Contains(ibp.ItemBankId))
                .ToDictionaryAsync(ibp => ibp.ItemBankId, ibp => ibp.Id);

            var data = questions.ConvertAll(q =>
            {
                itemBankPointsLookup.TryGetValue(q.ItemBankId, out var itemBankPointId);

                var subCount = q.QuestionTypeId == (long)OES.Helper.Enums.QuestionType.Comprehension
                    ? (q.SubQuestions?.Count > 0 ? q.SubQuestions.Count : 1)
                    : 1;

                return new ManualQuestionsPaginationResponseDto(
                    q.Id,
                    q.QuestionDetails.FirstOrDefault(qd => qd.LanguageId == paper.LanguageId)?.Body ?? "",
                    q.Code,
                    q.QuestionType.Name,
                    itemBankPointId,
                    q.ItemBankId,
                    q.ItemBank.Name,
                    q.DifficultyLevelId,
                    q.DifficultyLevel.Name,
                    q.FormQuestions?.Count(fq => fq.Form?.PaperId == paperId) ?? 0,
                    subCount
                );
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                new CustomTableData<ManualQuestionsPaginationResponseDto>(data, total));
        }

        #endregion Helper Methods
    }
}