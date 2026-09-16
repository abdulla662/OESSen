using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.ParallelService;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Services.Services
{
    public class EquationTemplateService : IEquationTemplateService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly ParallelQueryService _parallelQueryService;
        private readonly ILogger<EquationTemplateService> _logger;

        public EquationTemplateService(
            ICommonService commonService,
            FilterParamsValues filterParamsValues,
            ParallelQueryService parallelQueryService,
            ILogger<EquationTemplateService> logger)
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _parallelQueryService = parallelQueryService;
            _logger = logger;
        }

        public async Task<ApiResponse> GetAllEquationTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetAll()
                .AsNoTracking();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(a => a.Name.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            var templatesQuery = query.Include(et => et.EquationCategories);

            var projectedQuery = templatesQuery.Select(et => new
            {
                et.Id,
                et.Name,
                et.CreationDate,
                EquationCount = et.EquationCategories.Count,
                CategoryNames = string.Join(", ", et.EquationCategories.Select(ec => ec.CategoryName))
            });

            projectedQuery = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? projectedQuery.OrderByDescending(x => x.CreationDate)
                : projectedQuery.OrderBy(x => x.CreationDate);

            var totalRecords = await projectedQuery.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoEquationTemplateFound
                );
            }

            var paginatedEquationTemplates = await projectedQuery
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            var equationTemplateDtos = paginatedEquationTemplates.ConvertAll(et => new EquationTemplatePaginationDto
            {
                Id = et.Id,
                Name = et.Name,
                EquationCount = et.EquationCount,
                CategoryNames = et.CategoryNames,
                CreationDate = et.CreationDate
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EquationTemplatesRetrievedSuccessfully,
                new CustomTableData<EquationTemplatePaginationDto>(equationTemplateDtos, totalRecords)
            );
        }

        public async Task<ApiResponse> GetEquationTemplateById(long id)
        {
            const string includes = $"{nameof(EquationTemplate.EquationCategories)}," +
                                    $"{nameof(EquationTemplate.PaperItemBankEquation)}.{nameof(PaperItemBankEquation.Paper)}," +
                                    $"{nameof(EquationTemplate.PaperItemBankEquation)}.{nameof(PaperItemBankEquation.ItemBank)}," +
                                    $"{nameof(EquationTemplate.PaperItemBankEquation)}.{nameof(PaperItemBankEquation.Form)}," +
                                    $"{nameof(EquationTemplate.PaperItemBankEquation)}.{nameof(PaperItemBankEquation.EquationCategory)}";

            var equationTemplate = await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetObjAsync(et => et.Id == id, Including: includes);

            if (equationTemplate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoEquationTemplateFound
                );
            }

            if (equationTemplate.EquationCategories?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoEquationCategoriesFoundForThisTemplate
                );
            }

            var firstRelation = equationTemplate.PaperItemBankEquation?.FirstOrDefault();

            if (firstRelation == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoItemBankRelationsFoundForThisTemplate
                );
            }

            var paper = firstRelation.Paper;
            var form = firstRelation.Form;

            var categoryItemBanks = equationTemplate
                .PaperItemBankEquation
                .Where(pie => pie.ItemBank != null)
                .GroupBy(pie => pie.EquationCategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(pie => new GetItemBankDto
                    {
                        Id = pie.ItemBankId,
                        Name = pie.ItemBank.Name
                    })
                    .DistinctBy(ib => ib.Id)
                    .ToList()
                );

            var response = new GetEquationTemplateResponseDto
            {
                Id = equationTemplate.Id,
                Name = equationTemplate.Name,
                TotalEquation = equationTemplate.TotalEquation,
                PaperId = firstRelation.PaperId,
                PaperName = paper?.Name ?? Resource.Unknown,
                PaperType = paper.Type,
                FormId = firstRelation.FormId,
                FormName = form?.Name ?? Resource.Unknown,
                Equations = [.. equationTemplate.EquationCategories.Select(ec => new GetEquationDto
                {
                    Id = ec.Id,
                    Name = ec.CategoryName,
                    Equation = ec.Equation,
                    ShowInResults = ec.ShowInResults,
                    DisplayEquation = EquationCategoryValidator.BuildDisplayEquation(ec.Equation, id => categoryItemBanks.ContainsKey(ec.Id)
                        ? categoryItemBanks[ec.Id].FirstOrDefault(x => x.Id == id)?.Name
                        : null),
                    ItemBanks = categoryItemBanks.ContainsKey(ec.Id)
                        ? categoryItemBanks[ec.Id]
                        : []
                })]
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EquationTemplatesRetrievedSuccessfully,
                response
            );
        }

        //public async Task<ApiResponse> GetUserEquationGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Equation) &&
        //        (
        //            g.CreationUser.ToLower() == currentUser ||
        //            g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
        //        ),
        //        Including: nameof(OESGroup.GroupResources)
        //    );

        //    var groupDtos = groups
        //        .Select(g => new GetOESGroupDto
        //        {
        //            Id = g.Id,
        //            Name = g.Name
        //        })
        //        .DistinctBy(x => x.Id)
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        groupDtos
        //    );
        //}

        //public async Task<ApiResponse> GetEquationGroupsAsync(long EquationId)
        //{
        //    var equationGroups = await _commonService
        //        ._unitOfWork
        //        .Repository<EquationGroups, long>()
        //        .GetAllAsync(
        //            x => x.EquationId == EquationId &&
        //                 !x.OESGroup.IsTemplate,
        //            Including: nameof(EquationGroups.OESGroup)
        //        );

        //    var equationGroupsDto = new EquationGroupDto();

        //    equationGroups.ToList().ForEach(ibg =>
        //        equationGroupsDto.GroupsIds.Add(ibg.OESGroupId));

        //    equationGroupsDto.OwnerGroupId = equationGroups
        //        .FirstOrDefault(x =>
        //            x.OESGroup != null &&
        //            x.OESGroup.AutoCreatedForUser
        //        )?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        equationGroupsDto
        //    );
        //}

        public async Task<ApiResponse> GetCandidateQuestionsWithEquation(long formId)
        {
            // 1. Fetch template equation
            var templateEquation = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .GetAll()
                .AsNoTracking()
                .Include(p => p.EquationTemplate)
                .Where(e => e.FormId == formId)
                .Select(e => e.EquationTemplate)
                .FirstOrDefaultAsync();

            if (templateEquation == null)
                return CreateNotFoundResponse(Resource.NoTemplateEquationFoundForThisForm);

            // 2. Fetch all questions for this form
            var allQuestions = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsWithEquationView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(q => q.FormId == formId)
                .ToListAsync();

            if (allQuestions.Count == 0)
                return CreateNotFoundResponse(Resource.NoQuestionsFoundForThisForm);

            // 3. Calculate metrics in memory from questions (replaces view query)
            var itemBankMetrics = CalculateMetricsFromQuestionsView(allQuestions, templateEquation.Id);

            if (itemBankMetrics.Count == 0)
                return CreateNotFoundResponse(Resource.NoQuestionsFoundForThisForm);

            // 4. Get unique questions for display
            var questions = allQuestions
                .GroupBy(q => q.QuestionId)
                .Select(g => g.First())
                .ToList();

            // 5. Get all category equations
            var allCategoryEquations = await GetAllCategoryEquationsAsync(templateEquation.Id);

            // 6. Group metrics by (CandidateID, TrialNumber, RegistrationId) for easy lookup
            var metricsByCandidate = itemBankMetrics
                .GroupBy(m => new { m.CandidateID, m.TrialNumber, m.RegistrationId })
                .ToDictionary(
                    g => (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId),
                    g => g.ToList()
                );

            // 6. Calculate categories and scores per candidate trial
            var allCategoryCalculations = new Dictionary<string, CategoryCalculationDto>();
            decimal finalScore = 0;

            var categoryLookup = await _commonService._unitOfWork
                .Repository<EquationCategory, long>()
                .GetAll()
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.CategoryName);

            // Process each candidate trial
            foreach (var candidateTrial in metricsByCandidate.Keys)
            {
                var metrics = metricsByCandidate[candidateTrial];

                var candidateQuestions = questions
                    .Where(q => q.CandidateID == candidateTrial.CandidateID && q.TrialNumber == candidateTrial.TrialNumber && q.RegistrationId == candidateTrial.RegistrationId)
                    .ToList();

                var categoryCalcs = CalculateCategories(candidateQuestions, metrics, allCategoryEquations, categoryLookup);

                // Process total equation: first replace category placeholders, then resolve item bank metrics
                finalScore = ProcessTemplateEquation(ReplaceMetricsInEquation(templateEquation.TotalEquation, metrics), categoryCalcs);

                // Store calculations for the latest trial
                allCategoryCalculations = categoryCalcs;
            }

            var processedTemplateEquation = ReplaceEquationPlaceholders(ReplaceMetricsInEquation(templateEquation.TotalEquation, metricsByCandidate.Values.LastOrDefault()), allCategoryCalculations);

            // Also resolve item bank metrics in the processed equation for display
            if (metricsByCandidate.Count > 0)
            {
                var lastMetrics = metricsByCandidate.Values.Last();
                processedTemplateEquation = ReplaceMetricsInEquation(processedTemplateEquation, lastMetrics);
            }

            // 7. Prepare response
            var result = new CandidateEquationResultDto
            {
                Questions = questions.ConvertAll(MapToDto),
                CategoryCalculations = allCategoryCalculations,
                CandidateItemBankMetrics = GroupMetricsByCandidate(itemBankMetrics),
                FinalScore = finalScore,
                TemplateEquation = templateEquation.TotalEquation,
                ProcessedTemplateEquation = processedTemplateEquation
            };

            return new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = string.Format(Resource.EquationsCalculatedSuccessfullyFinalScore, finalScore.ToString("F2")),
                Data = result
            };
        }

        public async Task<ApiResponse> AddEquationTemplateAsync(AddOrUpdateEquationTemplateDto addOrUpdateEquationTemplateDto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(addOrUpdateEquationTemplateDto.Name))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.TemplateNameIsRequired
                );
            }

            if (addOrUpdateEquationTemplateDto.Equations?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.AtLeastOneEquationCategoryIsRequired
                );
            }

            var categoryNames = addOrUpdateEquationTemplateDto.Equations.ConvertAll(e => e.Name.Trim().ToLower());

            if (categoryNames.Count != categoryNames.Distinct().Count())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.DuplicateCategoryNamesAreNotAllowed
                );
            }

            var categoryPattern = new Regex(@"\[([^\]]+)\]");

            foreach (var equation in addOrUpdateEquationTemplateDto.Equations)
            {
                foreach (Match match in categoryPattern.Matches(equation.Equation))
                {
                    if (!IsMatchInsideFunction(equation.Equation, match.Index))
                    {
                        var referencedName = match.Groups[1].Value;

                        if (!addOrUpdateEquationTemplateDto.Equations.Any(e => e.Name.Equals(referencedName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return _commonService._apiResponse.GetApiResponse(
                                CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                string.Format(Resource.CategoryReferencesNonExistentCategory, equation.Name, referencedName)
                            );
                        }
                    }
                }
            }

            var allItemBankIds = addOrUpdateEquationTemplateDto.Equations
                .Where(e => e.ItemBankIds != null)
                .SelectMany(e => e.ItemBankIds)
                .Distinct()
                .ToList();

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == addOrUpdateEquationTemplateDto.PaperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.Id == addOrUpdateEquationTemplateDto.FormId && f.PaperId == addOrUpdateEquationTemplateDto.PaperId);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.NotFound,
                    Resource.FormNotFoundOrDoesNotBelongToTheSelectedPaper
                );
            }

            var existingTemplate = await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetObjAsync(x => x.Name.ToLower() == addOrUpdateEquationTemplateDto.Name.Trim().ToLower());

            if (existingTemplate != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.EquationWithNameAlreadyExists, addOrUpdateEquationTemplateDto.Name)
                );
            }

            var existingTemplateForPaperForm = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .GetObjAsync(x => x.PaperId == addOrUpdateEquationTemplateDto.PaperId && x.FormId == addOrUpdateEquationTemplateDto.FormId);

            if (existingTemplateForPaperForm != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.AnEquationTemplateAlreadyExistsForThisPaperAndForm
                );
            }

            if (allItemBankIds.Count != 0)
            {
                var existingItemBanks = await _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .GetAllAsync(ib => allItemBankIds.Contains(ib.Id));

                if (existingItemBanks.Count() != allItemBankIds.Count)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.BadRequest,
                        Resource.OneOrMoreItemBanksNotFound
                    );
                }
            }

            await using var transaction = await _commonService._unitOfWork.BeginTransactionAsync();

            var equationTemplate = new EquationTemplate
            {
                Name = addOrUpdateEquationTemplateDto.Name.Trim(),
                TotalEquation = addOrUpdateEquationTemplateDto.TotalEquation
            };

            await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .AddAsync(equationTemplate);

            if (await _commonService._unitOfWork.Complete() <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToSaveEquationTemplate);
            }

            var allRelations = new List<PaperItemBankEquation>();

            foreach (var equationDto in addOrUpdateEquationTemplateDto.Equations)
            {
                var equationCategory = new EquationCategory
                {
                    EquationTemplateId = equationTemplate.Id,
                    CategoryName = equationDto.Name.Trim(),
                    Equation = equationDto.Equation.Trim(),
                    ShowInResults = equationDto.ShowInResults
                };

                await _commonService
                    ._unitOfWork
                    .Repository<EquationCategory, long>()
                    .AddAsync(equationCategory);

                if (await _commonService._unitOfWork.Complete() <= 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.InternalServerError,
                        Resource.FailedToSaveEquationCategories
                    );
                }

                if (equationDto.ItemBankIds?.Count > 0)
                {
                    var relations = equationDto.ItemBankIds.ConvertAll(itemBankId => new PaperItemBankEquation
                    {
                        PaperId = addOrUpdateEquationTemplateDto.PaperId,
                        ItemBankId = itemBankId,
                        FormId = addOrUpdateEquationTemplateDto.FormId,
                        EquationTemplateId = equationTemplate.Id,
                        EquationCategoryId = equationCategory.Id
                    });

                    allRelations.AddRange(relations);
                }
            }

            if (allRelations.Count != 0)
            {
                await _commonService
                    ._unitOfWork
                    .Repository<PaperItemBankEquation, long>()
                    .AddRangeAsync(allRelations);

                if (await _commonService._unitOfWork.Complete() <= 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.InternalServerError,
                        Resource.FailedToSaveEquationTemplate);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EquationTemplateCreatedSuccessfully
            );
        }

        public async Task<ApiResponse> UpdateEquationTemplateAsync(AddOrUpdateEquationTemplateDto request, CancellationToken cancellationToken = default)
        {
            if (request.Id <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.TemplateIdIsRequiredForUpdate
                );
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.TemplateNameIsRequired
                );
            }

            if (request.Equations == null || request.Equations.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.AtLeastOneEquationCategoryIsRequired
                );
            }

            var categoryNames = request.Equations.ConvertAll(e => e.Name.Trim().ToLower());

            if (categoryNames.Count != categoryNames.Distinct().Count())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.DuplicateCategoryNamesAreNotAllowed
                );
            }

            foreach (var equation in request.Equations)
            {
                var categoryPattern = new Regex(@"\[([^\]]+)\]");

                var matches = categoryPattern.Matches(equation.Equation);

                foreach (Match match in matches)
                {
                    var referencedName = match.Groups[1].Value;

                    // Check if it's NOT inside a function (like NC([ItemBank]))
                    bool isInsideFunction = IsMatchInsideFunction(equation.Equation, match.Index);

                    if (!isInsideFunction)
                    {
                        // It's a category reference - validate it exists
                        bool categoryExists = request.Equations.Any(e => e.Name.Equals(referencedName, StringComparison.OrdinalIgnoreCase));

                        if (!categoryExists)
                        {
                            return _commonService._apiResponse.GetApiResponse(
                                CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                string.Format(Resource.CategoryReferencesNonExistentCategory, equation.Name, referencedName)
                            );
                        }
                    }
                }
            }

            // Only validate if there are item banks to validate
            // Prepare item bank IDs (may be empty for categories that only use other categories)
            var allItemBankIds = request
                .Equations
                .Where(e => e.ItemBankIds != null)
                .SelectMany(e => e.ItemBankIds)
                .Distinct()
                .ToList();

            var equationTemplate = await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetObjAsync(et => et.Id == request.Id, Including: $"{nameof(EquationTemplate.EquationCategories)},{nameof(EquationTemplate.PaperItemBankEquation)}");

            if (equationTemplate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoEquationTemplateFound
                );
            }

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == request.PaperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.Id == request.FormId && f.PaperId == request.PaperId);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.NotFound,
                    Resource.FormNotFoundOrDoesNotBelongToTheSelectedPaper
                );
            }

            var existingTemplateName = await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetObjAsync(x => x.Name.ToLower() == request.Name.ToLower() && x.Id != request.Id);

            if (existingTemplateName != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.EquationWithNameAlreadyExists, request.Name)
                );
            }

            if (allItemBankIds.Count > 0)
            {
                var existingItemBanks = await _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .GetAllAsync(ib => allItemBankIds.Contains(ib.Id));

                if (existingItemBanks.Count() != allItemBankIds.Count)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.BadRequest,
                        Resource.OneOrMoreItemBanksNotFound
                    );
                }
            }

            await using var transaction = await _commonService._unitOfWork.BeginTransactionAsync();

            if (equationTemplate.Name != request.Name || equationTemplate.TotalEquation != request.TotalEquation)
            {
                equationTemplate.Name = request.Name;
                equationTemplate.TotalEquation = request.TotalEquation;
                _commonService._unitOfWork.Repository<EquationTemplate, long>().Update(equationTemplate);
            }

            // Get existing categories
            var existingCategories = equationTemplate.EquationCategories?.ToList() ?? [];
            var existingRelations = equationTemplate.PaperItemBankEquation?.ToList() ?? [];

            // Detect changes in categories
            var categoriesToDelete = new List<EquationCategory>();
            var categoriesToUpdate = new List<EquationCategory>();
            var categoriesToAdd = new List<EquationCategory>();

            // Check for deleted or updated categories
            foreach (var existingCat in existingCategories)
            {
                var matchingRequest = request.Equations.FirstOrDefault(e => e.Id == existingCat.Id);

                if (matchingRequest == null)
                {
                    // Category was deleted
                    categoriesToDelete.Add(existingCat);
                }
                else if (existingCat.CategoryName != matchingRequest.Name || existingCat.Equation != matchingRequest.Equation.Trim() || existingCat.ShowInResults != matchingRequest.ShowInResults)
                {
                    // Category was updated
                    existingCat.CategoryName = matchingRequest.Name;
                    existingCat.Equation = matchingRequest.Equation.Trim();
                    existingCat.ShowInResults = matchingRequest.ShowInResults;
                    categoriesToUpdate.Add(existingCat);
                }
            }

            // Check for new categories (negative IDs or IDs not in existing)
            foreach (var requestEq in request.Equations)
            {
                if (requestEq.Id <= 0 || !existingCategories.Any(ec => ec.Id == requestEq.Id))
                {
                    categoriesToAdd.Add(new EquationCategory
                    {
                        EquationTemplateId = equationTemplate.Id,
                        CategoryName = requestEq.Name,
                        Equation = requestEq.Equation.Trim(),
                        ShowInResults = requestEq.ShowInResults
                    });
                }
            }

            // Handle category deletions
            if (categoriesToDelete.Count != 0)
            {
                // Delete relations for deleted categories
                var deletedCategoryIds = categoriesToDelete.Select(c => c.Id).ToHashSet();

                var relationsToDelete = existingRelations
                    .Where(r => deletedCategoryIds.Contains(r.EquationCategoryId))
                    .ToList();

                if (relationsToDelete.Count > 0)
                {
                    _commonService._unitOfWork.Repository<PaperItemBankEquation, long>().DeleteRange(relationsToDelete);
                }

                _commonService._unitOfWork.Repository<EquationCategory, long>().DeleteRange(categoriesToDelete);
            }

            // Handle category updates
            if (categoriesToUpdate.Count != 0)
            {
                _commonService._unitOfWork.Repository<EquationCategory, long>().UpdateRange(categoriesToUpdate);
            }

            // Handle category additions
            if (categoriesToAdd.Count != 0)
            {
                await _commonService._unitOfWork.Repository<EquationCategory, long>().AddRangeAsync(categoriesToAdd);
            }

            // Save category changes to get new IDs if needed
            if (categoriesToDelete.Count != 0 || categoriesToUpdate.Count != 0 || categoriesToAdd.Count != 0)
            {
                await _commonService._unitOfWork.Complete();
            }

            // Now handle item bank relations
            // Merge existing categories with new ones for relation updates
            var allCurrentCategories = existingCategories
                .Where(ec => !categoriesToDelete.Contains(ec))
                .Union(categoriesToAdd)
                .ToList();

            // Check for item bank relation changes
            var relationsToDeleteList = new List<PaperItemBankEquation>();
            var relationsToAddList = new List<PaperItemBankEquation>();

            foreach (var requestEq in request.Equations)
            {
                var category = allCurrentCategories.FirstOrDefault(c => c.Id == requestEq.Id || (requestEq.Id <= 0 && c.CategoryName == requestEq.Name.Trim()));

                if (category == null) continue;

                var existingCategoryRelations = existingRelations.Where(r => r.EquationCategoryId == category.Id).ToList();

                var existingItemBankIds = existingCategoryRelations.Select(r => r.ItemBankId).ToHashSet();

                // Handle null or empty ItemBankIds for categories that only use other categories
                var requestItemBankIds = (requestEq.ItemBankIds != null && requestEq.ItemBankIds.Count != 0)
                    ? requestEq.ItemBankIds.ToHashSet()
                    : [];

                // Find relations to delete
                var itemBanksToRemove = existingItemBankIds.Except(requestItemBankIds).ToList();
                if (itemBanksToRemove.Count != 0)
                {
                    relationsToDeleteList.AddRange(existingCategoryRelations.Where(r => itemBanksToRemove.Contains(r.ItemBankId)));
                }

                // Find relations to add
                var itemBanksToAdd = requestItemBankIds.Except(existingItemBankIds).ToList();
                if (itemBanksToAdd.Count != 0)
                {
                    relationsToAddList.AddRange(itemBanksToAdd.Select(itemBankId => new PaperItemBankEquation
                    {
                        PaperId = request.PaperId,
                        ItemBankId = itemBankId,
                        FormId = request.FormId,
                        EquationTemplateId = equationTemplate.Id,
                        EquationCategoryId = category.Id
                    }));
                }
            }

            // Apply relation changes
            if (relationsToDeleteList.Count != 0)
            {
                _commonService._unitOfWork.Repository<PaperItemBankEquation, long>().DeleteRange(relationsToDeleteList);
            }

            if (relationsToAddList.Count != 0)
            {
                await _commonService._unitOfWork.Repository<PaperItemBankEquation, long>().AddRangeAsync(relationsToAddList);
            }

            await _commonService._unitOfWork.Complete();
            await transaction.CommitAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EquationTemplateUpdatedSuccessfully
            );
        }

        public async Task<ApiResponse> DeleteEquationTemplateAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidTemplateId
                );
            }

            var equationTemplate = await _commonService
                ._unitOfWork
                .Repository<EquationTemplate, long>()
                .GetObjAsync(et => et.Id == id, Including: $"{nameof(EquationTemplate.EquationCategories)},{nameof(EquationTemplate.PaperItemBankEquation)}");

            if (equationTemplate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoEquationTemplateFound
                );
            }

            await using var transaction = await _commonService._unitOfWork.BeginTransactionAsync();

            // Delete PaperItembankEquation relations first
            if (equationTemplate.PaperItemBankEquation?.Any() == true)
            {
                _commonService
                    ._unitOfWork
                    .Repository<PaperItemBankEquation, long>()
                    .DeleteRange(equationTemplate.PaperItemBankEquation);
            }

            // Delete equation categories
            if (equationTemplate.EquationCategories?.Any() == true)
            {
                _commonService
                    ._unitOfWork
                    .Repository<EquationCategory, long>()
                    .DeleteRange(equationTemplate.EquationCategories);
            }

            // Delete the template itself
            _commonService._unitOfWork
                .Repository<EquationTemplate, long>()
                .Delete(equationTemplate);

            if (await _commonService._unitOfWork.Complete() <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToDeleteEquationTemplate
                );
            }

            await transaction.CommitAsync(cancellationToken);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EquationTemplateDeletedSuccessfully
            );
        }

        public async Task<ApiResponse> GetCandidateResultsAsync(ExportCandidatesRequestDto request)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking();

            if (request.CandidateList != null && request.CandidateList.Any())
            {
                query = query.Where(x =>
                    request.CandidateList.Contains(x.CandidateCode) || request.CandidateList.Contains(x.CandidateNationalId)
                );
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.CandidateCodeIsRequired
                );
            }

            var results = await query
                .Select(x => new CandidateResultDto
                {
                    RegistrationId = x.RegistrationId,
                    ClientCandidateID = x.CandidateNationalId,
                    FirstName = x.CandidateDisplayName,
                    ExamSeriesCode = x.PaperCode,
                    ExamStartDate = x.CandidateExamDate,
                    FinalScore = x.FinalScore,
                    ReviewerAccount = x.ReviewedBy,
                    ReviewStatusEnum = x.ReviewStatus,
                    PaperFormCode = x.PaperCode,
                    PaperFormId = x.PaperFormIdActual,
                    PaperType = x.PaperType ?? PaperType.Standard,
                    SentToCTR = x.SentToCTR,
                    SentToCTRAt = x.SentToCTRAt,
                    CandidateStartedExam = x.CandidateStartedExam,
                    CandidateEndedExam = x.CandidateEndedExam,
                    ReviewStatus = (int)x.ReviewStatus,
                    ReviewedAt = x.ReviewedAt,
                    VenueCode = x.VenueCode,
                    TotalEquation = null
                })
                .ToListAsync();

            var paperEquations = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .GetAll()
                .AsNoTracking()
                .Include(x => x.Form)
                .Include(x => x.EquationTemplate)
                    .ThenInclude(t => t.EquationCategories)
                .Include(x => x.EquationCategory)
                .Where(x => results.Select(r => r.PaperFormId).Contains(x.FormId))
                .ToListAsync();

            var questionCategories = await _commonService
                ._unitOfWork
                .Repository<QuestionCategory, long>()
                .GetAll()
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.FinalScoreName ?? c.Name ?? c.Id.ToString());

            foreach (var result in results)
            {
                result.CategoryIdNameMap = questionCategories;
                result.ParsedFinalScore = ParseCalculatedFinalScore(result.FinalScore);

                var template = paperEquations
                    .Where(p => p.FormId == result.PaperFormId)
                    .Select(p => p.EquationTemplate)
                    .FirstOrDefault();

                result.TotalEquation = template?.TotalEquation;

                if (result.PaperType == PaperType.Standard)
                {
                    if (template?.EquationCategories != null)
                    {
                        result.CategoryEquations = [.. template
                            .EquationCategories
                            .Select(c => new CategoryCalculationDto
                            {
                                CategoryId = c.Id,
                                CategoryName = c.CategoryName,
                                OriginalEquation = c.Equation,
                                ProcessedEquation = c.Equation,
                                CalculatedValue = 0,
                                QuestionCount = 0,
                                CorrectQuestionCount = 0,
                                IncorrectQuestionCount = 0,
                                UnscoredQuestionCount = 0,
                                ShowInResults = c.ShowInResults
                            })
                        ];
                    }
                    else
                    {
                        result.CategoryEquations = [];
                    }
                }
                else
                {
                    result.CategoryEquations = [.. paperEquations
                        .Where(p => p.Form.Code == result.PaperFormCode)
                        .GroupBy(p => p.EquationCategoryId)
                        .Select(g => new CategoryCalculationDto
                        {
                            CategoryId = g.Key,
                            CategoryName = g.First().EquationCategory.CategoryName,
                            OriginalEquation = Resource.Average,
                            ProcessedEquation = Resource.Average,
                            CalculatedValue = 0,
                            QuestionCount = g.Count(),
                            CorrectQuestionCount = 0,
                            IncorrectQuestionCount = 0,
                            UnscoredQuestionCount = 0,
                            ShowInResults = g.First().EquationCategory.ShowInResults
                        })
                    ];
                }
            }

            if (results.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultsAvailable
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FetchSuccess,
                results
            );
        }

        public async Task<ApiResponse> GetAllItemBanksFromQuestionBlocksAsync(long paperId)
        {
            var itemBanks = await _commonService
                ._unitOfWork
                .Repository<ItemBanksFromQuestionsBlocksView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(ib => ib.PaperId == paperId)
                .Select(ib => new ItemBanksFromItemBankPointResponseDto
                {
                    Id = ib.ItemBankId,
                    Name = ib.ItemBankName
                })
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperItemBanksRetrievedSuccessfully,
                itemBanks
            );
        }

        #region ==================== COMMON METHODS - Shared by Both Modes ====================
        //
        // These helper methods are used by BOTH Simple Mode and Advanced Mode.
        //
        #endregion

        #region Common - Response Helpers

        private static ApiResponse CreateNotFoundResponse(string message)
        {
            return new ApiResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = message,
                Data = new CandidateEquationResultDto
                {
                    Questions = [],
                    CandidateItemBankMetrics = [],
                    FinalScore = 0,
                    TemplateEquation = ""
                }
            };
        }

        private static Dictionary<long, List<ItemBankMetricsDto>> GroupMetricsByCandidate(List<CalculateItemBankMetricsView> metrics)
        {
            return metrics
                .GroupBy(m => m.CandidateID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(m =>
                    {
                        return new ItemBankMetricsDto
                        {
                            CandidateID = m.CandidateID,
                            RegistrationId = m.RegistrationId,
                            ItemBankId = m.ItemBankId,
                            ItemBankName = m.ItemBankName,
                            NC = m.NC,
                            ND = m.ND,
                            NT = m.NT,
                            QC = m.QC,
                            CandidateCode = m.CandidateCode,
                            FirstName = m.FirstName,
                            Gender = m.Gender

                        };
                    })
                    .OrderBy(m => m.ItemBankName)
                    .ToList()
                );
        }

        /// <summary>
        /// Calculates item bank metrics (NC, ND, NT, QC) from questions data in memory.
        /// This replaces the need for the vw_calculateitembankmetrics database view.
        /// </summary>
        private static List<CalculateItemBankMetricsView> CalculateMetricsFromQuestionsView(
            List<CandidateQuestionsWithEquationView> questions,
            long equationTemplateId)
        {
            // Filter questions that have ItemBankId and match the template (same as view WHERE clause)
            var validQuestions = questions
                .Where(q => q.ItemBankId.HasValue && q.EquationTemplateId == equationTemplateId)
                .ToList();

            // Group by the same keys as the view and calculate aggregates
            var metrics = validQuestions
                .GroupBy(q => new
                {
                    q.TrialNumber,
                    q.RegistrationId,
                    EquationTemplateId = equationTemplateId,
                    q.FormId,
                    q.CandidateID,
                    q.CandidateCode,
                    ExamStartDate = q.ExamStartDate.HasValue ? DateOnly.FromDateTime(q.ExamStartDate.Value) : (DateOnly?)null,
                    q.FirstName,
                    q.ItemBankId,
                    q.ItemBankName,
                    q.Gender
                })
                .Select(g => new CalculateItemBankMetricsView
                {
                    TrialNumber = g.Key.TrialNumber,
                    RegistrationId = g.Key.RegistrationId,
                    EquationTemplateId = equationTemplateId,
                    FormId = g.Key.FormId,
                    CandidateID = g.Key.CandidateID,
                    CandidateCode = g.Key.CandidateCode,
                    ExamStartDate = g.Key.ExamStartDate,
                    FirstName = g.Key.FirstName,
                    ItemBankId = g.Key.ItemBankId,
                    ItemBankName = g.Key.ItemBankName,
                    Gender = g.Key.Gender,
                    // NC: Count of correct answers
                    NC = g.Count(q => q.IsCorrect),
                    // ND: Sum of FinalDeltaValue for correct answers
                    ND = (double)g.Where(q => q.IsCorrect).Sum(q => q.FinalDeltaValue),
                    // NT: Sum of all FinalDeltaValue
                    NT = (double)g.Sum(q => q.FinalDeltaValue),
                    // QC: Count of questions
                    QC = g.Count()
                })
                .ToList();

            return metrics;
        }

        private static CandidateQuestionsAnswersDto MapToDto(CandidateQuestionsWithEquationView q)
        {
            return new CandidateQuestionsAnswersDto
            {
                CandidateID = q.CandidateID,
                CandidateCode = q.CandidateCode,
                ClientCandidateID = q.ClientCandidateID,
                FirstName = q.FirstName,
                MiddleName = q.MiddleName,
                LastName = q.LastName,
                CandidateEmail = q.CandidateEmail,
                CandidatePhoneNumber = q.CandidatePhoneNumber,
                QuestionId = q.QuestionId,
                ParentQuestionId = q.ParentQuestionId,
                IsRootQuestion = q.IsRootQuestion,
                QuestionCode = q.QuestionCode,
                QuestionType = q.QuestionType,
                QuestionSubject = q.QuestionSubject,
                QuestionScore = q.QuestionScore,
                IsAutoCorrectable = q.IsAutoCorrectable,
                AnswerId = q.AnswerId,
                AnswerText = q.AnswerText,
                AnswerIds = q.AnswerIds,
                Visited = q.Visited,
                Answered = q.Answered,
                MarkedForReview = q.MarkedForReview,
                ElapsedTimeInSeconds = q.ElapsedTimeInSeconds,
                PaperFormId = q.FormId,
                PaperFormName = q.PaperName,
                AnswerCreatedAt = q.AnswerCreatedAt,
                AnswerLastModifiedAt = q.AnswerLastModifiedAt,
                CreatedAt = q.CreatedAt,
                ModelAnswerNumericValue = q.ModelAnswerNumericValue,
                CorrectAnswer = q.CorrectAnswer,
                FinalDeltaValue = q.FinalDeltaValue,
                ItemBankId = q.ItemBankId,
                ItemBankName = q.ItemBankName,
                EquationTemplateName = q.EquationTemplateName,
                TotalEquation = q.TotalEquation,
                EquationCategoryName = q.EquationCategoryName,
                OriginalEquation = q.OriginalEquation,
                KeyAnswer = q.KeyAnswer,
                Response = q.Response
            };
        }

        private async Task<Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>> GetAdaptiveCategoryScoresAsync(long formId, List<long> candidateIds)
        {
            var result = new Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>();

            var candidateExamDetails = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(ced => ced.PaperFormIdActual == formId && candidateIds.Contains(ced.CandidateId))
                .Select(ced => new { ced.CandidateId, ced.RegistrationId, ced.FinalScore })
                .ToListAsync();

            foreach (var candidate in candidateExamDetails)
            {
                if (string.IsNullOrWhiteSpace(candidate.FinalScore))
                    continue;

                // Parse JSON: [{1:90.2},{2:80.4}] or [{"1":90.2},{"2":80.4}]
                var categoryScores = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(candidate.FinalScore);

                if (categoryScores != null && categoryScores.Count > 0)
                {
                    var parsedScores = new Dictionary<long, decimal>();

                    foreach (var dict in categoryScores)
                    {
                        foreach (var kvp in dict)
                        {
                            if (long.TryParse(kvp.Key, out var categoryId))
                            {
                                parsedScores[categoryId] = kvp.Value;
                            }
                        }
                    }

                    if (parsedScores.Count > 0)
                    {
                        result[(candidate.CandidateId, candidate.RegistrationId)] = parsedScores;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Common - Category Calculationssp_GetCandidateQuestionsForExport

        /// <summary>
        /// [COMMON] Calculates category scores from questions and metrics.
        /// Used by both Simple Mode and Advanced Mode.
        /// </summary>
        private static Dictionary<string, CategoryCalculationDto> CalculateCategories(
            List<CandidateQuestionsWithEquationView> questions,
            List<CalculateItemBankMetricsView> metrics,
            Dictionary<string, (string Equation, bool ShowInResults)> allCategoryEquations,
            Dictionary<long, string> categoryIdToNameLookup
        )
        {
            var categoryCalculations = new Dictionary<string, CategoryCalculationDto>();

            // Group questions by category
            var categoryGroups = questions
                .Where(q => q.EquationCategoryId.HasValue)
                .OrderBy(q => q.EquationCategoryCreationDate)
                .GroupBy(q => q.EquationCategoryId.Value)
                .ToList();

            foreach (var categoryGroup in categoryGroups)
            {
                var categoryId = categoryGroup.Key;

                if (!categoryIdToNameLookup.TryGetValue(categoryId, out var categoryName))
                    continue;

                allCategoryEquations.TryGetValue(categoryName, out var catInfo);
                var categoryEquation = catInfo.Equation;

                if (string.IsNullOrWhiteSpace(categoryEquation))
                {
                    var emptyCategory = CreateEmptyCategory(categoryName, categoryGroup.Count());
                    emptyCategory.CorrectQuestionCount = categoryGroup.Count(q => q.IsCorrect);
                    emptyCategory.IncorrectQuestionCount = categoryGroup.Count(q => q.Answered && !q.IsCorrect);
                    emptyCategory.UnscoredQuestionCount = categoryGroup.Count(q => q.UnScored == true);
                    emptyCategory.ShowInResults = catInfo.ShowInResults;
                    categoryCalculations[categoryName] = emptyCategory;
                    continue;
                }

                // Calculate correct, incorrect, and unscored questions for this category
                var correctCount = categoryGroup.Count(q => q.IsCorrect);
                var incorrectCount = categoryGroup.Count(q => q.Answered && !q.IsCorrect);
                var unscoredCount = categoryGroup.Count(q => q.UnScored == true);

                // Process equation with metrics
                var processedEquation = ReplaceMetricsInEquation(categoryEquation, metrics);

                processedEquation = ReplaceCategoryPlaceholders(processedEquation, categoryCalculations);

                var categoryResult = Math.Max(0, EvaluateEquation(processedEquation));

                categoryCalculations[categoryName] = new CategoryCalculationDto
                {
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    OriginalEquation = EquationCategoryValidator.BuildDisplayEquation(categoryEquation, id => metrics.FirstOrDefault(m => m.ItemBankId == id)?.ItemBankName),
                    ProcessedEquation = processedEquation,
                    CalculatedValue = Convert.ToDecimal(categoryResult),
                    QuestionCount = categoryGroup.Count(),
                    CorrectQuestionCount = correctCount,
                    IncorrectQuestionCount = incorrectCount,
                    UnscoredQuestionCount = unscoredCount,
                    ShowInResults = catInfo.ShowInResults
                };
            }

            // Add missing categories
            AddMissingCategories(categoryCalculations, allCategoryEquations);

            return categoryCalculations;
        }

        private static CategoryCalculationDto CreateEmptyCategory(string categoryName, int questionCount)
        {
            return new CategoryCalculationDto
            {
                CategoryName = categoryName,
                OriginalEquation = "",
                ProcessedEquation = "",
                CalculatedValue = 0,
                QuestionCount = questionCount,
                CorrectQuestionCount = 0,
                IncorrectQuestionCount = 0,
                UnscoredQuestionCount = 0
            };
        }

        private static void AddMissingCategories(
            Dictionary<string, CategoryCalculationDto> categoryCalculations,
            Dictionary<string, (string Equation, bool ShowInResults)> allCategoryEquations
        )
        {
            foreach (var (categoryName, (equation, showInResults)) in allCategoryEquations)
            {
                if (categoryCalculations.ContainsKey(categoryName))
                    continue;

                var processedEquation = ReplaceCategoryPlaceholders(equation ?? "0", categoryCalculations);
                var calculatedValue = Math.Max(0, EvaluateEquation(processedEquation));

                categoryCalculations[categoryName] = new CategoryCalculationDto
                {
                    CategoryName = categoryName,
                    OriginalEquation = equation ?? "0",
                    ProcessedEquation = processedEquation,
                    CalculatedValue = Convert.ToDecimal(calculatedValue),
                    QuestionCount = 0,
                    CorrectQuestionCount = 0,
                    IncorrectQuestionCount = 0,
                    UnscoredQuestionCount = 0,
                    ShowInResults = showInResults
                };
            }
        }

        private async Task<Dictionary<string, (string Equation, bool ShowInResults)>> GetAllCategoryEquationsAsync(long templateId)
        {
            if (templateId == 0)
                return [];

            return await _commonService
                ._unitOfWork
                .Repository<EquationCategory, long>()
                .GetAll()
                .AsNoTracking()
                .Where(ec => ec.EquationTemplateId == templateId)
                .ToDictionaryAsync(
                    ec => ec.CategoryName,
                    ec => (ec.Equation ?? "0", ec.ShowInResults)
                );
        }

        #endregion

        #region Common - Equation Processing

        /// <summary>
        /// [COMMON] Replaces NC/ND/NT placeholders with actual metric values.
        /// Used by both Simple Mode and Advanced Mode.
        /// </summary>
        private static string ReplaceMetricsInEquation(string equation, List<CalculateItemBankMetricsView> metrics)
        {
            if (string.IsNullOrWhiteSpace(equation))
                return equation;

            var functionPattern = new Regex(@"(NC|ND|NT)\(\[(\d+)\]\)", RegexOptions.IgnoreCase);
            return functionPattern.Replace(equation, match =>
            {
                var metricType = match.Groups[1].Value.ToUpper();
                if (!int.TryParse(match.Groups[2].Value, out int itemBankId)) return "0";

                var metric = metrics.FirstOrDefault(m => m.ItemBankId == itemBankId);
                if (metric == null) return "0";

                // Return the appropriate metric value
                return metricType switch
                {
                    "NC" => metric.NC.ToString(CultureInfo.InvariantCulture),
                    "ND" => metric.ND.ToString(CultureInfo.InvariantCulture),
                    "NT" => metric.NT.ToString(CultureInfo.InvariantCulture),
                    _ => "0"
                };
            });
        }

        private static string ReplaceCategoryPlaceholders(string equation, Dictionary<string, CategoryCalculationDto> categoryCalculations)
        {
            if (string.IsNullOrWhiteSpace(equation))
                return equation;

            var categoryPattern = new Regex(@"\[([^\]]+)\]");

            return categoryPattern.Replace(equation, match =>
            {
                // Skip if inside a function like NC([ItemBank])
                if (IsMatchInsideFunction(equation, match.Index))
                    return match.Value;

                var categoryName = match.Groups[1].Value;

                return categoryCalculations.TryGetValue(categoryName, out var category)
                    ? category.CalculatedValue.ToString(CultureInfo.InvariantCulture)
                    : "0";
            });
        }

        private static string ReplaceEquationPlaceholders(string templateEquation, Dictionary<string, CategoryCalculationDto> categoryCalculations)
        {
            if (string.IsNullOrEmpty(templateEquation) || categoryCalculations.Count == 0)
                return templateEquation ?? string.Empty;

            var result = templateEquation;

            // Sort by length descending to avoid partial replacements
            foreach (var (categoryName, category) in categoryCalculations.OrderByDescending(x => x.Key.Length))
            {
                if (string.IsNullOrEmpty(categoryName) || category == null)
                    continue;

                var placeholder = $"[{categoryName}]";
                var value = category.CalculatedValue.ToString(CultureInfo.InvariantCulture);
                result = result.Replace(placeholder, value);
            }

            return result;
        }

        private static decimal ProcessTemplateEquation(string templateEquation, Dictionary<string, CategoryCalculationDto> categoryCalculations)
        {
            if (string.IsNullOrEmpty(templateEquation))
                return 0;

            var processed = ReplaceEquationPlaceholders(templateEquation, categoryCalculations);

            return Convert.ToDecimal(EvaluateEquation(processed));
        }

        private static decimal EvaluateEquation(string equation)
        {
            if (string.IsNullOrWhiteSpace(equation))
                return 0;

            equation = equation.Replace("[", "").Replace("]", "").Trim();

            if (string.IsNullOrWhiteSpace(equation) || Regex.IsMatch(equation, @"[^0-9+\-*/(). ]"))
                return 0;

            try
            {
                var expression = new NCalc.Expression(equation);

                expression.EvaluateFunction += (name, args) =>
                {
                    args.HasResult = true;
                    args.Result = 0;
                };

                expression.EvaluateParameter += (name, args) =>
                {
                    args.HasResult = true;
                    args.Result = 0;
                };

                var result = expression.Evaluate();

                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsMatchInsideFunction(string equation, int matchIndex)
        {
            if (matchIndex < 3) return false;

            var beforeMatch = equation.Substring(0, matchIndex);

            var functionPattern = new Regex(@"(NC|ND|NT)\($", RegexOptions.IgnoreCase);

            return functionPattern.IsMatch(beforeMatch);
        }

        #endregion

        #region Common - DAT File Generation and Export
        //
        // These methods generate DAT files (cand.dat, item.dat, sect.dat, exam.dat)
        // and are used by BOTH Simple Mode and Advanced Mode.
        //
        // Entry Point: ExportCandidatesData (legacy single export - may still be used)
        // File Generators:
        //   - GenerateCandidateFile: Creates cand.dat
        //   - GenerateItemFile: Creates item.dat
        //   - GenerateSectionFile: Creates sect.dat
        //   - GenerateExamFile: Creates exam.dat
        //   - CreateExportZip: Packages files into ZIP
        //   - CalculateCandidateScores: Calculate scores for export
        //

        /// <summary>
        /// [LEGACY] Single export - exports data for one form/venue combination.
        /// Consider using batch methods for better performance.
        /// </summary>
        public async Task<ApiResponse> ExportCandidatesData(ExportCandidatesRequestDto request)
        {
            // Fetch questions for item - level details
            var questionsQuery = _commonService
              ._unitOfWork
              .Repository<CandidateQuestionsWithEquationView, long>()
              .GetAll()
              .AsNoTracking()
              .Where(q => q.FormId == request.FormId && q.VenueCode == request.VenueCode);
            // TODO: Exclude any candidate that one of his answers is EvaluationStatus 1 or 2 (NotEvaluated or PendingReview); Needs to add this to the view also.
            // TODO: Also add in the view, this field: CandidateExamDate.

            if (request.CandidateList?.Count > 0)
            {
                var registrationIds = request.CandidateList
                    .Where(id => long.TryParse(id, out _))
                    .Select(long.Parse)
                    .ToList();

                questionsQuery = questionsQuery.Where(q => registrationIds.Contains(q.RegistrationId) || request.CandidateList.Contains(q.ClientCandidateID));
            }

            if (request.StartDate.HasValue)
            {
                var startDate = request.StartDate.Value.Date;
                questionsQuery = questionsQuery.Where(q => q.ExamStartDate >= startDate);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                questionsQuery = questionsQuery.Where(q => q.ExamStartDate < endDate);
            }

            if (request.SentToCTR.HasValue)
            {
                var sentToCTR = request.SentToCTR;
                questionsQuery = questionsQuery.Where(q => q.SentToCTR == sentToCTR);
            }

            var questions = await questionsQuery.ToListAsync();

            if (questions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultsAvailable
                );
            }

            var venueCode = questions.FirstOrDefault()?.VenueCode ?? string.Empty;

            var questionLookup = questions
                .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.RegistrationId })
                .ToDictionary(g => (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId), g => g.First());

            // Fetch template equation and paper details
            var templateEquationWithPaper = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .GetAll()
                .Include(p => p.EquationTemplate)
                .Include(p => p.Paper)
                .AsNoTracking()
                .Where(e => e.FormId == request.FormId)
                .Select(e => new { e.EquationTemplate, e.Paper })
                .FirstOrDefaultAsync();

            if (templateEquationWithPaper?.EquationTemplate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoTemplatesFound
                );
            }

            var templateEquation = templateEquationWithPaper.EquationTemplate;
            var paperType = templateEquationWithPaper.Paper?.Type ?? PaperType.Standard;
            var paperSubtype = templateEquationWithPaper.Paper?.AdaptiveSubtype ?? AdaptivePaperSubtype.None;

            // Get all category equations
            var allCategoryEquations = await GetAllCategoryEquationsAsync(templateEquation.Id);

            // Calculate metrics in memory from questions (replaces view query)
            var metrics = CalculateMetricsFromQuestionsView(questions, templateEquation.Id);

            if (metrics.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesFound
                );
            }

            var categoryLookup = await _commonService._unitOfWork
                .Repository<EquationCategory, long>()
                .GetAll()
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.CategoryName);

            // Calculate scores per candidate
            var candidateScores = CalculateCandidateScores(
                metrics,
                questions,
                templateEquation.TotalEquation,
                allCategoryEquations,
                categoryLookup
            );

            var candidateExamDetails = _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .Where(x => !x.IsDemo && x.PaperFormIdActual == request.FormId);

            foreach (var exam in candidateExamDetails)
            {
                var key = (exam.CandidateId, exam.TrialNumber, exam.RegistrationId);

                if (candidateScores.TryGetValue(key, out var scoreResult))
                {
                    exam.PaperType = paperType;

                    var generatedCategoryJson = scoreResult
                        .CategoryValues
                        .Select(c => new Dictionary<string, decimal>
                        {
                            { c.Key, c.Value.CalculatedValue } // Extract CalculatedValue from CategoryCalculationDto
                        })
                        .ToList();

                    var currentFinalScore = new List<Dictionary<string, decimal>>();

                    if (!string.IsNullOrWhiteSpace(exam.FinalScore))
                    {
                        currentFinalScore = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(
                            exam.FinalScore
                        );

                        currentFinalScore.RemoveAll(dict => dict.ContainsKey("FinalScore"));

                        var scorePairs = currentFinalScore
                            .SelectMany(dict => dict)
                            .Where(kvp => kvp.Key != "FinalScore") // exclude only the FinalScore entry
                            .ToList();

                        Dictionary<string, decimal> finalScore = new()
                        {
                            {
                                "FinalScore",
                                scorePairs.Count > 0
                                    ? scorePairs.Average(kvp => kvp.Value)
                                    : 0m
                            }
                        };

                        currentFinalScore.Add(finalScore);
                    }

                    var existingKeys = currentFinalScore
                        .SelectMany(dict => dict.Keys)
                        .ToHashSet();

                    var newItems = generatedCategoryJson
                        .ExceptBy(existingKeys, dict => dict.Keys.First())
                        .ToHashSet();

                    List<Dictionary<string, decimal>> finalScoreJson = currentFinalScore;

                    finalScoreJson.AddRange(newItems);

                    exam.FinalScore = JsonSerializer.Serialize(
                        finalScoreJson,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        }
                    );
                    exam.FScore = scoreResult.FinalScore;
                    exam.GeneratedAt = DateTimeHelper.Now;
                }
            }

            await _commonService._unitOfWork.Complete();

            // Fetch adaptive category scores from CandidateExamDetails if paper is adaptive
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>> adaptiveCategoryScores = null;

            if (paperType == PaperType.Adaptive)
            {
                var candidateIds = metrics.Select(m => m.CandidateID).Distinct().ToList();
                adaptiveCategoryScores = await GetAdaptiveCategoryScoresAsync(request.FormId, candidateIds);
            }

            var candidates = metrics
                .GroupBy(m => new { m.CandidateID, m.TrialNumber, m.RegistrationId })
                .Where(g => questionLookup.ContainsKey((g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId)))
                .Select(g =>
                {
                    var key = (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId);

                    var q = questionLookup.GetValueOrDefault(key);

                    return new
                    {
                        g.Key.CandidateID,
                        g.Key.TrialNumber,
                        g.First().CandidateCode,
                        g.First().FirstName,
                        Gender = q?.Gender ?? false,
                        ClientCandidateID = q?.ClientCandidateID ?? "",
                        MiddleName = q?.MiddleName ?? "",
                        LastName = q?.LastName ?? ".",
                        RegistrationId = q?.RegistrationId ?? 0,
                        TCID = q?.TCID ?? 0,
                        ExamSeriesCode = q?.ExamSeriesCode ?? "",
                        FormName = q?.FormName ?? "",
                        ExamLanguage = q?.ExamLanguage ?? "",
                        ExamStartDate = q?.ExamStartDate,
                        PaperName = q?.PaperName ?? "",
                        PassingScore = q?.PassingScore ?? 0
                    };
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesFound
                );
            }

            List<BlockCandidateAnswer> blocks = null;
            Dictionary<long, string> categoryNames = [];

            if (paperType == PaperType.Adaptive)
            {
                blocks = [.. _commonService
                    ._unitOfWork
                    .Repository<BlockCandidateAnswer, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .Include(b => b.Block)
                    .AsNoTracking()
                    .Where(bca => bca.PaperFormId == request.FormId)
                ];

                // Load category names for ALL adaptive papers (MST and STEP both have CES-synced scores)
                categoryNames = _commonService
                    ._unitOfWork
                    .Repository<QuestionCategory, long>()
                    .GetAll()
                    .AsNoTracking()
                    .ToDictionary(c => c.Id, c => c.FinalScoreName);
            }

            // Generate export files
            var dateNow = DateTimeHelper.Now.ToString("yyyy-MM-dd");
            var zipBytes = CreateExportZip(dateNow, candidates, questions, candidateScores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores);
            var examDate = questions.FirstOrDefault()?.ExamStartDate?.ToString("yyyy-MM-dd") ?? dateNow;
            var fileName = $"Nemr-{examDate}.zip";

            var candidateDat = GenerateCandidateFile(candidates);
            var itemDat = GenerateItemFile(questions, paperType);
            var sectDat = GenerateSectionFile(questions, candidates, candidateScores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores);
            var examDat = GenerateExamFile(questions, candidates, candidateScores, paperType, paperSubtype, adaptiveCategoryScores);

            return new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Message = string.Format(Resource.ExportCandidatesSuccessful, candidates.Count),
                Data = new ExportCandidatesResponseDto
                {
                    FileName = fileName,
                    FileContent = Convert.ToBase64String(zipBytes),
                    ContentType = "application/zip",
                    CandidateCount = candidates.Count,
                    RegistrationIds = candidates.ConvertAll(c => c.RegistrationId),
                    FormId = request.FormId,
                    VenueId = request.VenueId,
                    VenueCode = venueCode,
                    PaperId = questions.FirstOrDefault().PaperId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ExamSeriesCode = questions.FirstOrDefault()?.ExamSeriesCode ?? string.Empty,
                    CandidateDat = candidateDat,
                    ItemDat = itemDat,
                    SectDat = sectDat,
                    ExamDat = examDat
                }
            };
        }

        private static Dictionary<(long CandidateId, int Trial, long RegistrationId), CandidateScoreResult> CalculateCandidateScores(
            List<CalculateItemBankMetricsView> metrics,
            List<CandidateQuestionsWithEquationView> questions,
            string totalEquation,
            Dictionary<string, (string Equation, bool ShowInResults)> allCategoryEquations,
            Dictionary<long, string> categoryLookup
        )
        {
            var results = new Dictionary<(long, int, long), CandidateScoreResult>();

            // Group by candidate, trial and registration
            var groupedMetrics = metrics
                .GroupBy(m => (m.CandidateID, m.TrialNumber, m.RegistrationId))
                .ToList();

            foreach (var group in groupedMetrics)
            {
                var candidateQuestions = questions
                    .Where(q => q.CandidateID == group.Key.CandidateID && q.TrialNumber == group.Key.TrialNumber && q.RegistrationId == group.Key.RegistrationId)
                    .ToList();

                var categoryCalcs = CalculateCategories(candidateQuestions, [.. group], allCategoryEquations, categoryLookup);

                var finalScore = ProcessTemplateEquation(ReplaceMetricsInEquation(totalEquation, [.. group]), categoryCalcs);

                results[group.Key] = new CandidateScoreResult
                {
                    FinalScore = finalScore,
                    CategoryValues = categoryCalcs // Direct assignment - no transformation needed!
                };
            }

            return results;
        }

        private static byte[] CreateExportZip(
            string dateNow,
            dynamic candidates,
            List<CandidateQuestionsWithEquationView> questions,
            Dictionary<(long, int, long), CandidateScoreResult> scores,
            List<BlockCandidateAnswer> blocks,
            Dictionary<long, string> categoryNames,
            PaperType paperType,
            AdaptivePaperSubtype paperSubtype,
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>> adaptiveCategoryScores = null
        )
        {
            using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                var examDate = questions.FirstOrDefault()?.ExamStartDate?.ToString("yyyy-MM-dd") ?? dateNow;
                AddFileToZip(archive, $"cand-{examDate}.dat", GenerateCandidateFile(candidates));
                AddFileToZip(archive, $"item-{examDate}.dat", GenerateItemFile(questions, paperType));
                AddFileToZip(archive, $"sect-{examDate}.dat", GenerateSectionFile(questions, candidates, scores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores));
                AddFileToZip(archive, $"exam-{examDate}.dat", GenerateExamFile(questions, candidates, scores, paperType, paperSubtype, adaptiveCategoryScores));
            }

            zipStream.Position = 0;

            return zipStream.ToArray();
        }

        private static string GenerateCandidateFile(dynamic candidates)
        {
            var sb = new StringBuilder();

            sb.AppendLine("CandidateID\tClientCandidateID\tFirstName\tLastName\tMiddleName");

            foreach (var c in candidates)
            {
                sb.AppendLine($"{c.CandidateID}\t{c.ClientCandidateID}\t{c.FirstName}\t{c.LastName}\t{c.MiddleName}");
            }

            return sb.ToString();
        }

        private static string GenerateItemFile(List<CandidateQuestionsWithEquationView> questions, PaperType paperType)
        {
            var sb = new StringBuilder();

            sb.AppendLine("RegistrationID\tCandidateID\tClientCandidateID\tItemName\tSection\tType\tStatus\tScore\tScored\tTime\tKey\tResponse");

            /* The item status. The following values are possible:
             * (0: incorrect),
             * (1: correct),
             * (2: skipped),
             * (3: incomplete), // TODO: Not used currently
             * (4: never presented),
             * (5: inactive) // TODO: Not used currently
             */

            questions = [.. questions.OrderBy(q => q.SectionId).ThenBy(q => q.QuestionId)];

            foreach (var q in questions)
            {
                var status = true switch
                {
                    true when q.Answered && q.IsCorrect => ItemStatus.correct,
                    true when q.Answered && !q.IsCorrect => ItemStatus.incorrect,
                    true when !q.Answered && q.Visited => ItemStatus.skipped,
                    _ => ItemStatus.never_presented,
                };

                var score = q.IsCorrect ? paperType == PaperType.Adaptive ? (bool)!q.UnScored ? q.FinalDeltaValue : 0 : 1 : 0;

                int scored = q.UnScored == true ? 0 : 1;

                sb.AppendLine(
                    $"{q.RegistrationId}\t{q.CandidateID}\t{q.ClientCandidateID}\t{q.QuestionCode}\t{q.SectionName}\t" +
                    $"s\t{(int)status}\t{score}\t{scored}\t{q.ElapsedTimeInSeconds ?? 0:F0}\t" +
                    $"{q.KeyAnswer ?? ""}\t{q.Response ?? ""}"
                );
            }

            return sb.ToString();
        }

        private static string GenerateSectionFile(
            List<CandidateQuestionsWithEquationView> questions,
            dynamic candidates,
            Dictionary<(long, int, long), CandidateScoreResult> scores,
            List<BlockCandidateAnswer> blocks,
            Dictionary<long, string> categoryNames,
            PaperType paperType,
            AdaptivePaperSubtype paperSubtype,
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>> adaptiveCategoryScores = null
        )
        {
            var sb = new StringBuilder();

            sb.AppendLine("RegistrationID\tCandidateID\tClientCandidateID\tSectionName\tSection\tSectionScore\tSectionItemsCorrect\tSectionItemsIncorrect\tSectionItemsSkipped\tSectionItemsUnscored\tSectionTime");

            // ========== PART 1: STANDARD SECTIONS (BOTH STANDARD AND ADAPTIVE) ==========

            IEnumerable<IGrouping<dynamic, CandidateQuestionsWithEquationView>> sectionGroups;

            if (paperType == PaperType.Adaptive)
            {
                sectionGroups = questions
                    .OrderBy(q => q.SectionId)
                    .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.ClientCandidateID, q.RegistrationId, q.SectionName, q.Visited });
            }
            else
            {
                sectionGroups = questions
                    .OrderBy(q => q.SectionId)
                    .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.ClientCandidateID, q.RegistrationId, q.SectionId, q.SectionName });
            }

            foreach (var g in sectionGroups)
            {
                var correct = g.Count(q => q.IsCorrect);
                var incorrect = g.Count(q => q.Answered && !q.IsCorrect);
                var skipped = g.Count(q => q.Visited && !q.Answered);
                var unscored = g.Count(q => q.UnScored == true);
                var totalTime = g.Sum(q => q.ElapsedTimeInSeconds ?? 0) * 1000;
                var sectionTimeInMs = (long)(g.First().SectionTimeInMinutes) * 60 * 1000;
                var visited = g.Count(q => q.Visited);

                if (paperType == PaperType.Adaptive && totalTime == 0 && visited == 0) continue;

                var sectionId = paperType == PaperType.Adaptive
                    ? g.First().SectionId
                    : g.Key.SectionId;

                var sectionName = g.Key.SectionName ?? $"Section {sectionId}";

                sb.AppendLine(
                    $"{g.Key.RegistrationId}\t{g.Key.CandidateID}\t{g.Key.ClientCandidateID}\t" +
                    $"{sectionName ?? $"Section {sectionId}"}\t{sectionId}\t{correct}\t" +
                    $"{correct}\t{incorrect}\t{skipped}\t{unscored}\t{totalTime:F0}"
                );
            }

            // ========== PART 2: CATEGORY SECTIONS (Skip for STEP - uses CES scores from PART 3 instead) ==========
            if (paperSubtype != AdaptivePaperSubtype.STEP)
            {
                foreach (var c in candidates)
                {
                    if (scores.TryGetValue((c.CandidateID, c.TrialNumber, (long)c.RegistrationId), out var score))
                    {
                        foreach (var cat in score.CategoryValues)
                        {
                            if (!cat.Value.ShowInResults)
                                continue;

                            sb.AppendLine(
                                $"{c.RegistrationId}\t{c.CandidateID}\t{c.ClientCandidateID}\t{cat.Key}\t0\t" +
                                $"{cat.Value.CalculatedValue:0.######}\t{cat.Value.CorrectQuestionCount}\t" +
                                $"{cat.Value.IncorrectQuestionCount}\t{cat.Value.QuestionCount - cat.Value.CorrectQuestionCount - cat.Value.IncorrectQuestionCount}\t" +
                                $"{cat.Value.UnscoredQuestionCount}\t0"
                            );
                        }
                    }
                }
            }

            // ========== PART 3: ADAPTIVE-SPECIFIC ==========
            if (paperType == PaperType.Adaptive)
            {
                // Group blocks by (CandidateId, RegistrationNumber) to handle candidates with multiple assignments
                var blocksByCandidate = blocks?
                    .GroupBy(b => (b.CandidateId, b.RegistrationNumber))
                    .ToDictionary(g => g.Key, g => g.ToList()) ?? [];

                foreach (var c in candidates)
                {
                    // 3A: Adaptive Category Scores (both MST and STEP have CES-synced scores)
                    if (adaptiveCategoryScores?.TryGetValue((c.CandidateID, (long)c.RegistrationId), out Dictionary<long, decimal> categoryScoresForCandidate) == true)
                    {
                        foreach (var kvp in categoryScoresForCandidate)
                        {
                            var categoryName = categoryNames != null && categoryNames.TryGetValue(kvp.Key, out var name) ? name : $"Category {kvp.Key}";
                            sb.AppendLine(
                                $"{c.RegistrationId}\t{c.CandidateID}\t{c.ClientCandidateID}\t{categoryName}\t0\t" +
                                $"{kvp.Value:0.######}\t0\t0\t0\t0\t0"
                            );
                        }

                        // Final Score line - skip for STEP
                        if (paperSubtype != AdaptivePaperSubtype.STEP)
                        {
                            var finalScoreName = paperSubtype == AdaptivePaperSubtype.MST ? "قدرات - كلية" : "Final Score";
                            sb.AppendLine(
                                $"{c.RegistrationId}\t{c.CandidateID}\t{c.ClientCandidateID}\t{finalScoreName}\t0\t" +
                                $"{categoryScoresForCandidate.Average(q => q.Value):0.######}\t0\t0\t0\t0\t0"
                            );
                        }
                    }

                    // 3B: Block scores - lookup by (CandidateId, RegistrationNumber) to match correct assignment
                    if (blocksByCandidate.TryGetValue((c.CandidateID, c.RegistrationId.ToString()), out List<BlockCandidateAnswer> candidateBlocks))
                    {
                        foreach (var block in candidateBlocks)
                        {
                            int BlockUnscoredCount = block.TotalScore == 0 ? block.TotalQuestionsCount : 0;
                            sb.AppendLine(
                                $"{c.RegistrationId}\t{c.CandidateID}\t{c.ClientCandidateID}\t{block.Block.Code}\t{block.BlockId}\t" +
                                $"{block.TotalScore:0.######}\t{block.CorrectQuestionsCount}\t{block.IncorrectQuestionsCount}\t" +
                                $"{block.TotalQuestionsCount - block.CorrectQuestionsCount - block.IncorrectQuestionsCount}\t{BlockUnscoredCount}\t0"
                            );
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static string GenerateExamFile(
            List<CandidateQuestionsWithEquationView> questions,
            dynamic candidates,
            Dictionary<(long, int, long), CandidateScoreResult> scores,
            PaperType paperType,
            AdaptivePaperSubtype paperSubtype,
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>> adaptiveCategoryScores = null
        )
        {
            var sb = new StringBuilder();

            sb.AppendLine("RegistrationID\tCandidateID\tClientCandidateID\tTCID\tExamSeriesCode\tExamName\tExamRevision\tForm\tExamLanguage\tAttempt\tExamDate\tTimeUsed\tPassingScore\tScore\tGrade\tNoShow\tNDARefused\tCorrect\tIncorrect\tSkipped\tUnscored");

            foreach (var c in candidates)
            {
                var candidateQuestions = questions.Where(q => q.CandidateID == c.CandidateID && q.TrialNumber == c.TrialNumber && q.RegistrationId == c.RegistrationId).ToList();
                var totalSeconds = candidateQuestions.Sum(q => q.ElapsedTimeInSeconds ?? 0);
                var timeUsed = TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss");

                // Calculate final score based on paper type
                decimal finalScore = 0;
                Dictionary<long, decimal> categoryScoresForCandidate = null;

                if (paperType == PaperType.Adaptive && adaptiveCategoryScores?.TryGetValue((c.CandidateID, (long)c.RegistrationId), out categoryScoresForCandidate) == true)
                {
                    if (categoryScoresForCandidate != null && categoryScoresForCandidate.Count > 0)
                        finalScore = categoryScoresForCandidate.Values.Average();
                }
                else
                {
                    finalScore = scores.TryGetValue((c.CandidateID, c.TrialNumber, (long)c.RegistrationId), out var s) ? s.FinalScore : 0;
                }

                var totalCorrect = candidateQuestions.Count(q => q.IsCorrect);
                var totalIncorrect = candidateQuestions.Count(q => q.Answered && !q.IsCorrect);
                var totalSkipped = candidateQuestions.Count(q => !q.Answered);
                var totalUnscored = candidateQuestions.Count(q => q.UnScored == true);
                var examSeriesCode = c.ExamSeriesCode;
                var examStartDate = c.ExamStartDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTimeHelper.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var gender = c.Gender ? 'F' : 'M';
                switch (paperSubtype)
                {
                    case AdaptivePaperSubtype.MST:
                        examSeriesCode = $"NCA-APT-AD-P4-{gender}";
                        break;
                    case AdaptivePaperSubtype.STEP:
                        examSeriesCode = $"NCA-STEP-AD-{gender}";
                        finalScore *= 100;
                        break;
                }

                sb.AppendLine(
                    $"{c.RegistrationId}\t{c.CandidateID}\t{c.ClientCandidateID}\t{c.TCID}\t" +
                    $"{examSeriesCode}\t{c.PaperName ?? "Assessment Test"}\t1\t{c.FormName ?? "FORM"}\t" +
                    $"{c.ExamLanguage ?? "ENU"}\t1\t{examStartDate}\t" +
                    $"{timeUsed}\t{c.PassingScore}\t{finalScore:0.######}\tTaken\t0\tFALSE\t" +
                    $"{totalCorrect}\t{totalIncorrect}\t{totalSkipped}\t{totalUnscored}"
                );
            }

            return sb.ToString();
        }

        private static void AddFileToZip(ZipArchive archive, string fileName, string content)
        {
            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(content);
        }

        #endregion

        #region ==================== SIMPLE MODE - Result Generation ====================
        //
        // Simple Mode generates results for ALL forms/venues within a date range.
        // Uses optimized batch queries for best performance with large datasets.
        //
        // Entry Point: GenerateResultsBatchAsync (called from Blazor via GenerateResultsBatchAsync)
        // Helper Methods:
        //   - FetchAllDataForDateRangeAsync: Bulk fetch all data
        //   - GroupDataByCombination: Group data by Form/Venue
        //   - CalculateCandidateScoresFromMemory: Calculate scores in memory
        //   - CalculateMetricsFromQuestions: Calculate NC/ND/NT metrics
        //   - GetAdaptiveCategoryScoresFromMemory: Get adaptive scores
        //   - SerializeFinalScoreJson: Serialize score to JSON
        //   - BulkUpdateCandidateExamDetailsAsync: Batch update database
        //
        #endregion

        #region Simple Mode - Entry Point

        /// <summary>
        /// [SIMPLE MODE] Main entry point for batch result generation.
        /// Generates results for ALL forms/venues within a date range.
        /// Fetches all data in single queries and processes in memory for optimal performance.
        /// </summary>
        public async Task<ApiResponse> GenerateResultsBatchAsync(
            ExportCandidateRequestByDateDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "=== BATCH RESULT GENERATION STARTED === DateRange: {StartDate} to {EndDate}",
                request.StartDate, request.EndDate);

            var startDateTime = request.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDateTime = request.EndDate.ToDateTime(TimeOnly.MaxValue);

            // Step 1: Fetch ALL data with single queries (no N+1)
            var data = await FetchAllDataForDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);

            _logger.LogInformation(
                "[Step 1] Data Fetching completed - Questions: {QuestionsCount}, Metrics: {MetricsCount}, Templates: {TemplatesCount}, Blocks: {BlocksCount}",
                data.Questions.Count,
                data.Metrics.Count,
                data.Templates.Count,
                data.Blocks.Count);

            if (data.Questions.Count == 0)
            {
                _logger.LogWarning("=== BATCH RESULT GENERATION ENDED (No Data) ===");
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultsAvailable
                );
            }

            // Step 2: Group data in memory by (FormId, VenueCode)
            var combinations = GroupDataByCombination(data);

            _logger.LogInformation(
                "[Step 2] Data Grouping completed - Combinations: {CombinationsCount}",
                combinations.Count);

            // Step 3: Process each combination (no additional DB queries)
            var allResults = new List<ExportCandidatesResponseDto>();
            var allScoreUpdates = new List<CandidateScoreUpdate>();
            var processedCombinations = 0;
            var totalCandidatesProcessed = 0;

            foreach (var combo in combinations.Values)
            {
                if (combo.Template == null) continue;

                // Build question lookup for this combination
                var questionLookup = combo.Questions
                    .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.RegistrationId })
                    .ToDictionary(g => (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId), g => g.First());

                // Calculate scores using existing logic (with in-memory data)
                var candidateScores = CalculateCandidateScoresFromMemory(
                    combo.Questions,
                    combo.MetricsByCandidate ?? [],
                    combo.Template.TotalEquation,
                    combo.Template.CategoryEquations,
                    data.EquationCategoryLookup
                );

                // Get existing adaptive scores for this form (CES-synced scores with QuestionCategory.Id keys)
                var existingAdaptiveScoresForForm = data.ExistingAdaptiveScoresByForm.TryGetValue(combo.FormId, out var scores) ? scores : null;

                // Collect score updates for bulk save
                foreach (var score in candidateScores)
                {
                    Dictionary<long, decimal> adaptiveScores = null;

                    existingAdaptiveScoresForForm?.TryGetValue(
                         (score.Key.CandidateId, score.Key.RegistrationId),
                         out adaptiveScores
                    );

                    allScoreUpdates.Add(new CandidateScoreUpdate
                    {
                        CandidateId = score.Key.CandidateId,
                        TrialNumber = score.Key.Trial,
                        RegistrationId = score.Key.RegistrationId,
                        FormId = combo.FormId,
                        FinalScoreJson = SerializeFinalScoreJson(score.Value),
                        PaperType = combo.Template.PaperType,
                        PaperSubtype = combo.Template.PaperSubtype,
                        EquationFinalScore = score.Value.FinalScore,
                        AdaptiveCategoryScores = adaptiveScores
                    });
                }

                // Generate export result using existing file generation logic
                var exportResult = GenerateExportResultFromMemory(
                    combo,
                    candidateScores,
                    questionLookup,
                    data.CategoryNames,
                    existingAdaptiveScoresForForm,
                    request.StartDate,
                    request.EndDate
                );

                if (exportResult != null)
                {
                    allResults.Add(exportResult);
                    totalCandidatesProcessed += exportResult.CandidateCount;
                }

                processedCombinations++;
            }

            _logger.LogInformation(
                "[Step 3] Score Calculation & File Generation completed - Processed: {ProcessedCombinations} combinations, {CandidatesCount} candidates, {ScoreUpdates} score updates",
                processedCombinations,
                totalCandidatesProcessed,
                allScoreUpdates.Count);

            // Step 4: Bulk update CandidateExamDetails
            if (allScoreUpdates.Count > 0)
            {
                await BulkUpdateCandidateExamDetailsAsync(allScoreUpdates, cancellationToken);
            }

            _logger.LogInformation(
                "[Step 4] Bulk Database Update completed - Updated: {UpdateCount} records",
                allScoreUpdates.Count);

            if (allResults.Count == 0)
            {
                _logger.LogWarning("=== BATCH RESULT GENERATION ENDED (No Results) ===");

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesFound
                );
            }

            _logger.LogInformation(
                "=== BATCH RESULT GENERATION COMPLETED === Candidates: {TotalCandidates}, Results: {ResultsCount}",
                totalCandidatesProcessed,
                allResults.Count);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.ExportCandidatesSuccessful, allResults.Sum(r => r.CandidateCount)),
                allResults
            );
        }

        #endregion

        #region Simple Mode - Data Fetching

        /// <summary>
        /// [SIMPLE MODE] Fetches all required data for the date range in single queries.
        /// Uses ParallelQueryService to allow parallel execution with separate DbContext instances.
        /// </summary>
        private async Task<OptimizedResultData> FetchAllDataForDateRangeAsync(
    DateOnly startDate,
    DateOnly endDate,
    CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Fetch] Starting parallel data fetch for date range {StartDate} to {EndDate}", startDate, endDate);

            var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

            // QUERY 1: Using optimized view (removed correlated subqueries and GROUP BY)
            var questionsTask = _parallelQueryService.ExecuteReadAsync<CandidateQuestionsWithEquationView, long, List<OptimizedCandidateQuestionData>>(
                repository => repository
                    .GetAll()
                    .AsNoTracking()
                    .Where(q => q.CandidateExamDate <= endDateTime && q.SentToCTR != true)
                    .Select(q => new OptimizedCandidateQuestionData
                    {
                        CandidateID = q.CandidateID,
                        CandidateCode = q.CandidateCode ?? "",
                        ClientCandidateID = q.ClientCandidateID ?? "",
                        FirstName = q.FirstName ?? "",
                        MiddleName = q.MiddleName ?? "",
                        LastName = q.LastName ?? ".",
                        Gender = q.Gender,
                        TrialNumber = q.TrialNumber,
                        RegistrationId = q.RegistrationId,
                        TCID = q.TCID,
                        FormId = q.FormId,
                        FormName = q.FormName ?? "",
                        VenueId = q.VenueId,
                        VenueCode = q.VenueCode ?? "",
                        PaperId = q.PaperId,
                        PaperName = q.PaperName ?? "",
                        QuestionId = q.QuestionId,
                        QuestionCode = q.QuestionCode ?? "",
                        SectionName = q.SectionName ?? "",
                        SectionId = q.SectionId,
                        UnScored = q.UnScored,
                        SectionTimeInMinutes = q.SectionTimeInMinutes,
                        IsCorrect = q.IsCorrect,
                        Answered = q.Answered,
                        Visited = q.Visited,
                        FinalDeltaValue = q.FinalDeltaValue,
                        ElapsedTimeInSeconds = q.ElapsedTimeInSeconds,
                        KeyAnswer = q.KeyAnswer,
                        Response = q.Response,
                        ItemBankId = q.ItemBankId,
                        EquationTemplateId = q.EquationTemplateId,
                        EquationCategoryId = q.EquationCategoryId,
                        //OriginalEquation = q.OriginalEquation,
                        EquationCategoryCreationDate = q.EquationCategoryCreationDate,
                        ExamSeriesCode = q.ExamSeriesCode,
                        ExamLanguage = q.ExamLanguage,
                        PassingScore = q.PassingScore,
                        ItemBankName = null,
                        EquationCategoryName = null,
                        ExamStartDate = q.CandidateExamDate ?? startDateTime,
                    })
                    .ToListAsync(cancellationToken)
            );

            // QUERY 2: All equation templates (small dataset, fetch once)
            var templatesTask = _parallelQueryService.ExecuteReadAsync<PaperItemBankEquation, long, List<PaperItemBankEquation>>(
                repository => repository
                    .Query()
                    .AsNoTracking()
                    .Include(p => p.EquationTemplate)
                    .ThenInclude(et => et.EquationCategories)
                    .Include(p => p.Paper)
                    .ToListAsync(cancellationToken)
            );

            // QUERY 3: Block answers for adaptive papers
            var blocksTask = _parallelQueryService.ExecuteReadAsync<BlockCandidateAnswer, long, List<BlockCandidateAnswerData>>(
                repository => repository
                    .Query()
                    .Include(b => b.Block)
                    .AsNoTracking()
                    .Select(b => new BlockCandidateAnswerData
                    {
                        PaperFormId = b.PaperFormId,
                        CandidateId = b.CandidateId,
                        RegistrationNumber = b.RegistrationNumber,
                        BlockId = b.BlockId,
                        BlockCode = b.Block.Code ?? "",
                        TotalScore = b.TotalScore,
                        CorrectQuestionsCount = b.CorrectQuestionsCount,
                        IncorrectQuestionsCount = b.IncorrectQuestionsCount,
                        TotalQuestionsCount = b.TotalQuestionsCount
                    })
                    .ToListAsync(cancellationToken)
            );

            // QUERY 4: Category names for adaptive papers (QuestionCategory.Id -> FinalScoreName)
            var categoryNamesTask = _parallelQueryService.ExecuteReadAsync<QuestionCategory, long, Dictionary<long, string>>(
                repository => repository
                    .GetAll()
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Id, c => c.FinalScoreName ?? "", cancellationToken)
            );

            // QUERY 5: Existing FinalScore data for adaptive papers (CES-synced scores)
            // This reads the existing FinalScore column which contains CES adaptive scores
            // with QuestionCategory.Id as keys (e.g., [{"6": 53.13}, {"7": 50.67}])
            var existingFinalScoresTask = _parallelQueryService.ExecuteReadAsync<CandidateExamDetails, long, List<(long FormId, long CandidateId, long RegistrationId, string FinalScore)>>(
                repository => repository
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .AsNoTracking()
                    .Where(ced => ced.CandidateExamDate >= startDateTime && ced.CandidateExamDate <= endDateTime)
                    .Where(ced => !string.IsNullOrEmpty(ced.FinalScore))
                    .Select(ced => new ValueTuple<long, long, long, string>(ced.PaperFormIdActual, ced.CandidateId, ced.RegistrationId, ced.FinalScore))
                    .ToListAsync(cancellationToken)
            );

            // Execute all queries in parallel (each with its own DbContext)
            var itemBankLookupTask = _parallelQueryService.ExecuteReadAsync<ItemBank, long, Dictionary<long, string>>(
                repository => repository
                    .GetAll()
                    .AsNoTracking()
                    .ToDictionaryAsync(ib => ib.Id, ib => ib.Name, cancellationToken)
            );

            var categoryLookupTask = _parallelQueryService.ExecuteReadAsync<EquationCategory, long, Dictionary<long, string>>(
                repository => repository
                    .GetAll()
                    .AsNoTracking()
                    .ToDictionaryAsync(ec => ec.Id, ec => ec.CategoryName, cancellationToken)
            );

            await Task.WhenAll(questionsTask, templatesTask, blocksTask, categoryNamesTask, existingFinalScoresTask, itemBankLookupTask, categoryLookupTask);

            var questions = await questionsTask;
            var templateEntities = await templatesTask;
            var blocks = await blocksTask;
            var categoryNames = await categoryNamesTask;
            var existingFinalScores = await existingFinalScoresTask;
            var itemBankLookup = await itemBankLookupTask;
            var categoryLookup = await categoryLookupTask;

            foreach (var q in questions)
            {
                if (q.ItemBankId.HasValue && itemBankLookup.TryGetValue(q.ItemBankId.Value, out var ibName))
                    q.ItemBankName = ibName;

                if (q.EquationCategoryId.HasValue && categoryLookup.TryGetValue(q.EquationCategoryId.Value, out var ecName))
                    q.EquationCategoryName = ecName;
            }

            _logger.LogInformation(
                "[Fetch] All queries completed - Questions: {Q}, Templates: {T}, Blocks: {B}, Categories: {C}, ExistingScores: {E}",
                questions.Count,
                templateEntities.Count,
                blocks.Count,
                categoryNames.Count,
                existingFinalScores.Count);

            // Calculate metrics in memory from questions data (replaces slow vw_calculateitembankmetrics view)
            var metrics = CalculateMetricsFromQuestions(questions);
            _logger.LogInformation("[Fetch] Metrics calculated in memory - Count: {Count}", metrics.Count);

            // Build template lookup
            var templates = templateEntities
                .GroupBy(p => p.FormId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return new EquationTemplateData
                        {
                            TemplateId = first.EquationTemplateId,
                            TotalEquation = first.EquationTemplate?.TotalEquation ?? "",
                            PaperType = first.Paper?.Type ?? PaperType.Standard,
                            PaperSubtype = first.Paper?.AdaptiveSubtype ?? AdaptivePaperSubtype.None,
                            CategoryEquations = first.EquationTemplate?
                                .EquationCategories?
                                .ToDictionary(c => c.CategoryName, c => (c.Equation ?? "0", c.ShowInResults)) ?? []
                        };
                    }
                );

            // Parse existing FinalScore to extract CES adaptive scores (QuestionCategory.Id keys)
            // These are the adaptive scores synced from CES with numeric keys like "6", "7"
            var existingAdaptiveScoresByForm = ParseExistingAdaptiveScores(existingFinalScores);
            _logger.LogInformation("[Fetch] Existing adaptive scores parsed - Forms: {Count}", existingAdaptiveScoresByForm.Count);

            return new OptimizedResultData
            {
                Questions = questions,
                Metrics = metrics,
                Templates = templates,
                Blocks = blocks,
                CategoryNames = categoryNames,
                ExistingAdaptiveScoresByForm = existingAdaptiveScoresByForm,
                EquationCategoryLookup = categoryLookup
            };
        }

        /// <summary>
        /// Parses existing FinalScore JSON to extract CES adaptive scores.
        /// CES stores adaptive scores with QuestionCategory.Id as keys (numeric strings like "6", "7").
        /// Returns: FormId -> CandidateId -> (QuestionCategory.Id -> Score)
        /// </summary>
        private static Dictionary<long, Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>> ParseExistingAdaptiveScores(
            List<(long FormId, long CandidateId, long RegistrationId, string FinalScore)> existingFinalScores)
        {
            var result = new Dictionary<long, Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>>();

            foreach (var (formId, candidateId, registrationId, finalScoreJson) in existingFinalScores)
            {
                if (string.IsNullOrWhiteSpace(finalScoreJson))
                    continue;

                try
                {
                    // Parse JSON: [{"6": 53.13}, {"7": 50.67}, {"الحساب": 22.86}, ...]
                    var categoryScores = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(finalScoreJson);

                    if (categoryScores == null || categoryScores.Count == 0)
                        continue;

                    var parsedScores = new Dictionary<long, decimal>();

                    foreach (var dict in categoryScores)
                    {
                        foreach (var kvp in dict)
                        {
                            // Only parse numeric keys (QuestionCategory.Id from CES)
                            // Skip non-numeric keys like "الحساب" or "FinalScore"
                            if (long.TryParse(kvp.Key, out var categoryId))
                            {
                                parsedScores[categoryId] = kvp.Value;
                            }
                        }
                    }

                    if (parsedScores.Count > 0)
                    {
                        if (!result.ContainsKey(formId))
                        {
                            result[formId] = [];
                        }
                        result[formId][(candidateId, registrationId)] = parsedScores;
                    }
                }
                catch
                {
                    // Ignore parse errors for individual records
                }
            }

            return result;
        }

        #endregion

        #region Simple Mode - Data Grouping and Processing

        /// <summary>
        /// [SIMPLE MODE] Groups all data by (FormId, VenueCode) combination for in-memory processing.
        /// </summary>
        private static Dictionary<(long FormId, string VenueCode), FormVenueCombinationData> GroupDataByCombination(OptimizedResultData data)
        {
            // Group questions by (FormId, VenueCode)
            var questionsByCombo = data.Questions
                .GroupBy(q => (q.FormId, q.VenueCode))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Group metrics by FormId then by candidate
            var metricsByForm = data.Metrics
                .GroupBy(m => m.FormId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(m => (m.CandidateID, m.TrialNumber, m.RegistrationId))
                          .ToDictionary(inner => inner.Key, inner => inner.ToList())
                );

            // Group blocks by FormId then by (CandidateId, RegistrationNumber) to handle candidates with multiple assignments
            var blocksByForm = data.Blocks
                .GroupBy(b => b.PaperFormId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(b => (b.CandidateId, b.RegistrationNumber))
                          .ToDictionary(inner => inner.Key, inner => inner.ToList())
                );

            var result = new Dictionary<(long, string), FormVenueCombinationData>();

            foreach (var combo in questionsByCombo)
            {
                var formId = combo.Key.FormId;
                var questions = combo.Value;

                if (questions.Count == 0) continue;

                var firstQuestion = questions[0];

                result[combo.Key] = new FormVenueCombinationData
                {
                    FormId = formId,
                    VenueId = firstQuestion.VenueId,
                    VenueCode = combo.Key.VenueCode,
                    FormName = firstQuestion.FormName,
                    PaperId = firstQuestion.PaperId,
                    PaperName = firstQuestion.PaperName,
                    Questions = questions,
                    MetricsByCandidate = metricsByForm.GetValueOrDefault(formId),
                    BlocksByCandidate = blocksByForm.GetValueOrDefault(formId),
                    Template = data.Templates.GetValueOrDefault(formId)
                };
            }

            return result;
        }

        /// <summary>
        /// [SIMPLE MODE] Calculates candidate scores from in-memory data using existing calculation logic.
        /// </summary>
        private static Dictionary<(long CandidateId, int Trial, long RegistrationId), CandidateScoreResult> CalculateCandidateScoresFromMemory(
            List<OptimizedCandidateQuestionData> questions,
            Dictionary<(long CandidateID, int TrialNumber, long RegistrationId), List<OptimizedItemBankMetrics>> metricsByCandidate,
            string totalEquation,
            Dictionary<string, (string Equation, bool ShowInResults)> allCategoryEquations,
            Dictionary<long, string> categoryLookup
        )
        {
            var results = new Dictionary<(long, int, long), CandidateScoreResult>();

            // Group questions by candidate
            var questionsByCandidate = questions
                .GroupBy(q => (q.CandidateID, q.TrialNumber, q.RegistrationId))
                .ToList();

            foreach (var group in questionsByCandidate)
            {
                var candidateQuestions = group.ToList();
                var metrics = metricsByCandidate.GetValueOrDefault(group.Key) ?? [];

                // Convert metrics to view format for existing calculation methods
                var metricsAsView = metrics.ConvertAll(m => new CalculateItemBankMetricsView
                {
                    CandidateID = m.CandidateID,
                    TrialNumber = m.TrialNumber,
                    RegistrationId = m.RegistrationId,
                    ItemBankId = m.ItemBankId,
                    ItemBankName = m.ItemBankName,
                    NC = m.NC,
                    ND = m.ND,
                    NT = m.NT,
                    CandidateCode = m.CandidateCode,
                    FirstName = m.FirstName,
                    Gender = m.Gender
                });

                // Convert questions to view format for existing calculation methods
                var questionsAsView = candidateQuestions.ConvertAll(q => new CandidateQuestionsWithEquationView
                {
                    CandidateID = q.CandidateID,
                    TrialNumber = q.TrialNumber,
                    RegistrationId = q.RegistrationId,
                    QuestionId = q.QuestionId,
                    IsCorrect = q.IsCorrect,
                    Answered = q.Answered,
                    UnScored = q.UnScored,
                    EquationCategoryId = q.EquationCategoryId,
                    //OriginalEquation = q.OriginalEquation,
                    EquationCategoryCreationDate = q.EquationCategoryCreationDate,
                    ItemBankId = q.ItemBankId ?? 0,
                    ItemBankName = q.ItemBankName
                });

                // Use existing calculation logic
                var categoryCalcs = CalculateCategories(questionsAsView, metricsAsView, allCategoryEquations, categoryLookup);
                var finalScore = ProcessTemplateEquation(ReplaceMetricsInEquation(totalEquation, metricsAsView), categoryCalcs);

                results[group.Key] = new CandidateScoreResult
                {
                    FinalScore = finalScore,
                    CategoryValues = categoryCalcs
                };
            }

            return results;
        }

        /// <summary>
        /// Generates export result from in-memory data.
        /// </summary>
        private ExportCandidatesResponseDto? GenerateExportResultFromMemory(
            FormVenueCombinationData combo,
            Dictionary<(long CandidateId, int Trial, long RegistrationId), CandidateScoreResult> candidateScores,
            Dictionary<(long CandidateID, int TrialNumber, long RegistrationId), OptimizedCandidateQuestionData> questionLookup,
            Dictionary<long, string> categoryNames,
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>? existingAdaptiveScores,
            DateOnly startDate,
            DateOnly endDate)
        {
            if (combo.Questions.Count == 0 || combo.Template == null)
                return null;

            var paperType = combo.Template.PaperType;
            var paperSubtype = combo.Template.PaperSubtype;

            // Build candidates list from questions
            var candidates = combo.Questions
                .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.RegistrationId })
                .Where(g => questionLookup.ContainsKey((g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId)))
                .Select(g =>
                {
                    var q = questionLookup[(g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId)];
                    return new
                    {
                        g.Key.CandidateID,
                        g.Key.TrialNumber,
                        q.CandidateCode,
                        q.FirstName,
                        q.Gender,
                        q.ClientCandidateID,
                        q.MiddleName,
                        q.LastName,
                        q.RegistrationId,
                        q.TCID,
                        q.ExamSeriesCode,
                        q.FormName,
                        q.ExamLanguage,
                        q.ExamStartDate,
                        q.PaperName,
                        q.PassingScore
                    };
                })
                .ToList();

            if (candidates.Count == 0)
                return null;

            // Convert questions to view format for file generation
            var questionsAsView = combo.Questions.ConvertAll(q => new CandidateQuestionsWithEquationView
            {
                CandidateID = q.CandidateID,
                TrialNumber = q.TrialNumber,
                RegistrationId = q.RegistrationId,
                QuestionId = q.QuestionId,
                QuestionCode = q.QuestionCode,
                SectionId = q.SectionId,
                SectionName = q.SectionName,
                SectionTimeInMinutes = q.SectionTimeInMinutes,
                IsCorrect = q.IsCorrect,
                Answered = q.Answered,
                Visited = q.Visited,
                UnScored = q.UnScored,
                FinalDeltaValue = q.FinalDeltaValue,
                ElapsedTimeInSeconds = q.ElapsedTimeInSeconds,
                KeyAnswer = q.KeyAnswer,
                Response = q.Response,
                ClientCandidateID = q.ClientCandidateID
            });

            // Get adaptive category scores if applicable
            // Use existing adaptive scores from CES (read from database FinalScore column)
            // These have QuestionCategory.Id as keys (e.g., {6: 53.13, 7: 50.67})
            // This matches the old code behavior which reads back from database after save
            Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>? adaptiveCategoryScores = null;
            if (paperType == PaperType.Adaptive)
            {
                adaptiveCategoryScores = existingAdaptiveScores;
            }

            // Get blocks for this form
            List<BlockCandidateAnswer>? blocks = null;
            if (paperType == PaperType.Adaptive && combo.BlocksByCandidate != null)
            {
                blocks = combo.BlocksByCandidate.Values
                    .SelectMany(b => b)
                    .Select(b => new BlockCandidateAnswer
                    {
                        PaperFormId = b.PaperFormId,
                        CandidateId = b.CandidateId,
                        RegistrationNumber = b.RegistrationNumber,
                        BlockId = b.BlockId,
                        TotalScore = b.TotalScore,
                        CorrectQuestionsCount = b.CorrectQuestionsCount,
                        IncorrectQuestionsCount = b.IncorrectQuestionsCount,
                        TotalQuestionsCount = b.TotalQuestionsCount,
                        Block = new Block { Code = b.BlockCode }
                    })
                    .ToList();
            }

            // Use existing file generation methods
            var dateNow = DateTimeHelper.Now.ToString("yyyy-MM-dd");
            var examDate = combo.Questions.FirstOrDefault()?.ExamStartDate?.ToString("yyyy-MM-dd") ?? dateNow;

            var candidateDat = GenerateCandidateFile(candidates);
            var itemDat = GenerateItemFile(questionsAsView, paperType);
            var sectDat = GenerateSectionFile(questionsAsView, candidates, candidateScores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores);
            var examDat = GenerateExamFile(questionsAsView, candidates, candidateScores, paperType, paperSubtype, adaptiveCategoryScores);

            // Create ZIP
            var zipBytes = CreateExportZip(dateNow, candidates, questionsAsView, candidateScores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores);
            var fileName = $"Nemr-{examDate}.zip";

            return new ExportCandidatesResponseDto
            {
                FileName = fileName,
                FileContent = Convert.ToBase64String(zipBytes),
                ContentType = "application/zip",
                CandidateCount = candidates.Count,
                RegistrationIds = candidates.ConvertAll(c => c.RegistrationId),
                FormId = combo.FormId,
                VenueId = combo.VenueId,
                VenueCode = combo.VenueCode,
                PaperId = combo.PaperId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MaxValue),
                ExamSeriesCode = combo.Questions.FirstOrDefault()?.ExamSeriesCode ?? "",
                CandidateDat = candidateDat,
                ItemDat = itemDat,
                SectDat = sectDat,
                ExamDat = examDat
            };
        }

        /// <summary>
        /// Extracts adaptive category scores from calculated results.
        /// </summary>
        private static Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>? GetAdaptiveCategoryScoresFromMemory(
            Dictionary<(long CandidateId, int Trial, long RegistrationId), CandidateScoreResult> candidateScores)
        {
            var result = new Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>();

            foreach (var kvp in candidateScores)
            {
                var categoryScores = new Dictionary<long, decimal>();
                foreach (var cat in kvp.Value.CategoryValues)
                {
                    categoryScores[cat.Value.CategoryId] = cat.Value.CalculatedValue;
                }
                if (categoryScores.Count > 0)
                {
                    result[(kvp.Key.CandidateId, kvp.Key.RegistrationId)] = categoryScores;
                }
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// [SIMPLE MODE] Serializes the final score to JSON format matching existing structure.
        /// Uses CategoryName (c.Key) as JSON key - same as original code and Advanced Mode.
        /// </summary>
        private static string SerializeFinalScoreJson(CandidateScoreResult score)
        {
            // Output format: [{"الحساب": 22.86}, {"الجبر": 0}, ..., {"FinalScore": 30.71}]
            var categoryJson = score.CategoryValues
                .Select(c => new Dictionary<string, decimal> { { c.Key, c.Value.CalculatedValue } })
                .ToList();

            categoryJson.Add(new Dictionary<string, decimal> { { "FinalScore", score.FinalScore } });

            return JsonSerializer.Serialize(categoryJson, new JsonSerializerOptions { WriteIndented = false });
        }

        #endregion

        #region Simple Mode - Database Update

        /// <summary>
        /// [SIMPLE MODE] Bulk updates CandidateExamDetails with calculated scores.
        /// Processes in batches of 500 for optimal performance.
        /// </summary>
        private async Task BulkUpdateCandidateExamDetailsAsync(
       List<CandidateScoreUpdate> updates,
       CancellationToken cancellationToken)
        {
            const int batchSize = 500;

            foreach (var batch in updates.Chunk(batchSize))
            {
                var batchList = batch.ToList();

                // Build lookup for fast matching
                var updateLookup = batchList.ToDictionary(
                    u => (u.CandidateId, u.TrialNumber, u.RegistrationId),
                    u => u
                );

                var candidateIds = batchList.Select(u => u.CandidateId).Distinct().ToList();
                var registrationIds = batchList.Select(u => u.RegistrationId).Distinct().ToList();
                var formIds = batchList.Select(u => u.FormId).Distinct().ToList();

                // Fetch existing records
                var records = await _commonService
                    ._unitOfWork
                    .Repository<CandidateExamDetails, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .Where(x => candidateIds.Contains(x.CandidateId) && registrationIds.Contains(x.RegistrationId) && formIds.Contains(x.PaperFormIdActual))
                    .ToListAsync(cancellationToken);

                // Apply updates
                foreach (var record in records)
                {
                    var key = (record.CandidateId, record.TrialNumber, record.RegistrationId);
                    if (updateLookup.TryGetValue(key, out var update))
                    {
                        // Calculate final score based on paper type (same logic as GenerateExamFile)
                        decimal calculatedFinalScore;

                        if (update.PaperType == PaperType.Adaptive && update.AdaptiveCategoryScores?.Count > 0)
                            calculatedFinalScore = update.AdaptiveCategoryScores.Values.Average();
                        else
                            calculatedFinalScore = update.EquationFinalScore;

                        if (update.PaperSubtype == AdaptivePaperSubtype.STEP)
                            calculatedFinalScore *= 100;

                        // Merge with existing FinalScore if present
                        if (!string.IsNullOrEmpty(record.FinalScore))
                        {
                            try
                            {
                                var existing = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(record.FinalScore) ?? [];
                                var newScores = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(update.FinalScoreJson) ?? [];

                                // Remove old FinalScore entry
                                existing.RemoveAll(d => d.ContainsKey("FinalScore"));

                                // Add new entries that don't exist
                                var existingKeys = existing.SelectMany(d => d.Keys).ToHashSet();
                                foreach (var newEntry in newScores)
                                {
                                    var key2 = newEntry.Keys.First();

                                    if (key2 == "FinalScore") continue;

                                    if (!existingKeys.Contains(key2))
                                    {
                                        existing.Add(newEntry);
                                    }
                                }

                                // Recalculate FinalScore as average of all category values and append FinalScore
                                existing.Add(new Dictionary<string, decimal> { { "FinalScore", calculatedFinalScore } });

                                record.FinalScore = JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = false });
                                record.FScore = calculatedFinalScore;
                                record.GeneratedAt = DateTimeHelper.Now;
                            }
                            catch
                            {
                                record.FinalScore = update.FinalScoreJson;
                            }
                        }
                        else
                        {
                            record.FinalScore = update.FinalScoreJson;
                        }

                        record.PaperType = update.PaperType;
                    }
                }

                await _commonService._unitOfWork.Complete();
            }
        }

        #endregion

        #region Simple Mode - Metrics Calculation

        /// <summary>
        /// [SIMPLE MODE] Calculates item bank metrics (NC, ND, NT) from questions data in memory.
        /// This replaces the slow vw_calculateitembankmetrics view query.
        /// </summary>
        private static List<OptimizedItemBankMetrics> CalculateMetricsFromQuestions(List<OptimizedCandidateQuestionData> questions)
        {
            // Filter questions that have ItemBankId and EquationTemplateId (same as view WHERE clause)
            var validQuestions = questions
                .Where(q => q.ItemBankId.HasValue && q.EquationTemplateId.HasValue)
                .ToList();

            // Group by the same keys as the view and calculate aggregates
            var metrics = validQuestions
                .GroupBy(q => new
                {
                    q.TrialNumber,
                    q.RegistrationId,
                    q.EquationTemplateId,
                    q.FormId,
                    q.CandidateID,
                    q.CandidateCode,
                    ExamStartDate = q.ExamStartDate.HasValue ? DateOnly.FromDateTime(q.ExamStartDate.Value) : (DateOnly?)null,
                    q.FirstName,
                    q.ItemBankId,
                    q.ItemBankName,
                    q.Gender
                })
                .Select(g => new OptimizedItemBankMetrics
                {
                    TrialNumber = g.Key.TrialNumber,
                    RegistrationId = g.Key.RegistrationId,
                    EquationTemplateId = g.Key.EquationTemplateId!.Value,
                    FormId = g.Key.FormId,
                    CandidateID = g.Key.CandidateID,
                    CandidateCode = g.Key.CandidateCode ?? "",
                    ExamStartDate = g.Key.ExamStartDate,
                    FirstName = g.Key.FirstName ?? "",
                    ItemBankId = g.Key.ItemBankId,
                    ItemBankName = g.Key.ItemBankName ?? "",
                    Gender = g.Key.Gender,
                    // NC: Count of correct answers
                    NC = g.Count(q => q.IsCorrect),
                    // ND: Sum of FinalDeltaValue for correct answers
                    ND = (double)g.Where(q => q.IsCorrect).Sum(q => q.FinalDeltaValue),
                    // NT: Sum of all FinalDeltaValue
                    NT = (double)g.Sum(q => q.FinalDeltaValue)
                })
                .ToList();

            return metrics;
        }

        #endregion

        #region ==================== ADVANCED MODE - Result Generation ====================
        //
        // Advanced Mode generates results for SELECTED forms/venues within a date range.
        // User selects specific Paper -> Forms -> Venues combinations.
        //
        // Entry Point: ExportCandidatesDataBatchAsync (called from Blazor via ExportCandidatesDataBatchAsync)
        // Helper Methods:
        //   - UpdateCandidateExamDetailsInMemory: Update scores in pre-fetched entities
        //   - CalculateMetricsFromQuestionsView: Calculate NC/ND/NT/QC metrics (uses view entities)
        //   - CalculateCandidateScores: Calculate scores using view entities
        //
        // Note: Uses different business logic than Simple Mode for FinalScore calculation:
        //   - Uses CategoryName as JSON key (vs CategoryId in Simple Mode)
        //   - Calculates FinalScore as average of existing scores (vs equation result in Simple Mode)
        //
        #endregion

        #region Advanced Mode - Entry Point

        /// <summary>
        /// [ADVANCED MODE] Main entry point for batch export.
        /// Generates results for SELECTED forms/venues within a date range.
        /// Fetches all data once and processes each combination using original business logic.
        /// </summary>
        public async Task<ApiResponse> ExportCandidatesDataBatchAsync(
     ExportCandidatesBatchRequestDto request,
     CancellationToken cancellationToken = default)
        {
            if (request.Combinations == null || request.Combinations.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InvalidParameter,
                    HttpStatusCode.BadRequest,
                    Resource.NoCandidatesFound
                );
            }

            _logger.LogInformation(
                "=== ADVANCED MODE BATCH EXPORT STARTED === Combinations: {Count}, DateRange: {StartDate} to {EndDate}",
                request.Combinations.Count, request.StartDate, request.EndDate);

            var equationCategoryLookup = await _commonService._unitOfWork
                .Repository<EquationCategory, long>()
                .GetAll()
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.CategoryName, cancellationToken);

            var startDateTime = request.StartDate.Date;
            var endDateTime = request.EndDate.Date.AddDays(1);

            // Build set of selected (FormId, VenueCode) for filtering
            var selectedCombinations = request.Combinations
                .Select(c => (c.FormId, c.VenueCode))
                .ToHashSet();

            var selectedFormIds = request.Combinations.Select(c => c.FormId).Distinct().ToList();

            // BULK FETCH 1: All questions for date range and selected forms
            var questionsQuery = _commonService._unitOfWork
                .Repository<CandidateQuestionsWithEquationView, long>()
                .GetAll()
                .AsNoTracking()
                .Where(q => q.CandidateExamDate >= startDateTime && q.CandidateExamDate <= endDateTime)
                .Where(q => selectedFormIds.Contains(q.FormId))
                .Where(q => request.SentToCTR == null || q.SentToCTR == request.SentToCTR);

            if (request.CandidateList != null && request.CandidateList.Any())
            {
                var registrationIds = request.CandidateList
                    .Where(id => long.TryParse(id, out _))
                    .Select(long.Parse)
                    .ToList();

                questionsQuery = questionsQuery.Where(q =>
                    registrationIds.Contains(q.RegistrationId) ||
                    request.CandidateList.Contains(q.ClientCandidateID)
                );
            }

            List<CandidateQuestionsWithEquationView> allQuestions;

            try
            {
                allQuestions = await questionsQuery.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR fetching questions: {Message} ===", ex.Message);
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.AnErrorOccurred
                );
            }

            if (allQuestions.Count == 0)
            {
                _logger.LogWarning("=== No data found for the given candidates ===");
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultsAvailable
                );
            }

            _logger.LogInformation("  [Fetch] Questions loaded: {Count}", allQuestions.Count);

            // Filter to only selected combinations
            var filteredQuestions = allQuestions
                .Where(q => selectedCombinations.Contains((q.FormId, q.VenueCode ?? "")))
                .ToList();

            if (filteredQuestions.Count == 0)
            {
                _logger.LogWarning("=== ADVANCED MODE BATCH EXPORT ENDED (No matching combinations) ===");
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoResultsAvailable
                );
            }

            _logger.LogInformation("  [Fetch] Filtered questions: {Count}", filteredQuestions.Count);

            // BULK FETCH 2: All templates for selected forms
            var allTemplates = await _commonService._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .GetAll()
                .AsNoTracking()
                .Include(p => p.EquationTemplate)
                    .ThenInclude(et => et.EquationCategories)
                .Include(p => p.Paper)
                .Where(p => selectedFormIds.Contains(p.FormId))
                .ToListAsync(cancellationToken);

            var templatesByForm = allTemplates
                .GroupBy(t => t.FormId)
                .ToDictionary(g => g.Key, g => g.First());

            _logger.LogInformation("  [Fetch] Templates loaded: {Count}", templatesByForm.Count);

            // BULK FETCH 3: All blocks for adaptive papers
            var adaptiveFormIds = templatesByForm
                .Where(t => t.Value.Paper?.Type == PaperType.Adaptive)
                .Select(t => t.Key)
                .ToList();

            var allBlocks = adaptiveFormIds.Count > 0
                ? await _commonService._unitOfWork
                    .Repository<BlockCandidateAnswer, long>()
                    .Query(applySignature: false, applyOrganizationIdFilter: false)
                    .Include(b => b.Block)
                    .AsNoTracking()
                    .Where(b => adaptiveFormIds.Contains(b.PaperFormId))
                    .ToListAsync(cancellationToken)
                : [];

            var blocksByForm = allBlocks
                .GroupBy(b => b.PaperFormId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // BULK FETCH 4: Category names for adaptive papers
            var categoryNames = adaptiveFormIds.Count > 0
                ? await _commonService._unitOfWork
                    .Repository<QuestionCategory, long>()
                    .GetAll()
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Id, c => c.FinalScoreName ?? "", cancellationToken)
                : [];

            _logger.LogInformation("  [Fetch] Blocks: {BlockCount}, Categories: {CategoryCount}",
                 allBlocks.Count, categoryNames.Count);

            // BULK FETCH 5: All CandidateExamDetails for selected forms (for updates)
            var allCandidateExamDetails = await _commonService._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .Where(x => !x.IsDemo && selectedFormIds.Contains(x.PaperFormIdActual))
                .ToListAsync(cancellationToken);

            var candidateExamDetailsByForm = allCandidateExamDetails
                .GroupBy(x => x.PaperFormIdActual)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("  [Fetch] CandidateExamDetails loaded: {Count}", allCandidateExamDetails.Count);

            // BULK FETCH 6: Adaptive category scores for all adaptive forms
            var adaptiveCategoryScoresByForm = new Dictionary<long, Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>>();
            if (adaptiveFormIds.Count > 0)
            {
                var adaptiveCandidateDetails = allCandidateExamDetails
                    .Where(ced => adaptiveFormIds.Contains(ced.PaperFormIdActual) && !string.IsNullOrWhiteSpace(ced.FinalScore))
                    .ToList();

                foreach (var formId in adaptiveFormIds)
                {
                    var formCandidates = adaptiveCandidateDetails
                        .Where(ced => ced.PaperFormIdActual == formId)
                        .ToList();

                    var formScores = new Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>>();
                    foreach (var candidate in formCandidates)
                    {
                        try
                        {
                            var categoryScores = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(candidate.FinalScore);
                            if (categoryScores != null && categoryScores.Count > 0)
                            {
                                var parsedScores = new Dictionary<long, decimal>();
                                foreach (var dict in categoryScores)
                                {
                                    foreach (var kvp in dict)
                                    {
                                        if (long.TryParse(kvp.Key, out var categoryId))
                                        {
                                            parsedScores[categoryId] = kvp.Value;
                                        }
                                    }
                                }
                                if (parsedScores.Count > 0)
                                {
                                    formScores[(candidate.CandidateId, candidate.RegistrationId)] = parsedScores;
                                }
                            }
                        }
                        catch { /* Skip invalid JSON */ }
                    }
                    adaptiveCategoryScoresByForm[formId] = formScores;
                }
            }

            // Group questions by (FormId, VenueCode)
            var questionsByCombo = filteredQuestions
                .GroupBy(q => (q.FormId, VenueCode: q.VenueCode ?? ""))
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("  [Process] Processing {Count} combinations", questionsByCombo.Count);

            // Process each combination using SAME business logic as ExportCandidatesData
            var allResults = new List<ExportCandidatesResponseDto>();

            foreach (var combo in questionsByCombo)
            {
                var formId = combo.Key.FormId;
                var venueCode = combo.Key.VenueCode;
                var questions = combo.Value;

                // Get template for this form
                if (!templatesByForm.TryGetValue(formId, out var templateEquationWithPaper) ||
                    templateEquationWithPaper.EquationTemplate == null)
                {
                    continue;
                }

                var templateEquation = templateEquationWithPaper.EquationTemplate;
                var paperType = templateEquationWithPaper.Paper?.Type ?? PaperType.Standard;
                var paperSubtype = templateEquationWithPaper.Paper?.AdaptiveSubtype ?? AdaptivePaperSubtype.None;

                // Get category equations (same as ExportCandidatesData)
                var allCategoryEquations = templateEquation.EquationCategories?
                      .ToDictionary(c => c.CategoryName, c => (c.Equation ?? "0", c.ShowInResults)) ?? [];

                // Calculate metrics using SAME method as ExportCandidatesData
                var metrics = CalculateMetricsFromQuestionsView(questions, templateEquation.Id);

                if (metrics.Count == 0) continue;

                // Build question lookup
                var questionLookup = questions
                 .GroupBy(q => new { q.CandidateID, q.TrialNumber, q.RegistrationId })
                 .ToDictionary(g => (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId), g => g.First());

                // Calculate scores using SAME method as ExportCandidatesData
                var candidateScores = CalculateCandidateScores(
                    metrics,
                    questions,
                    templateEquation.TotalEquation,
                    allCategoryEquations,
                    equationCategoryLookup);

                // Update CandidateExamDetails in-memory (using pre-fetched data) - SAME logic as ExportCandidatesData
                var venueCombo = request.Combinations.FirstOrDefault(c => c.FormId == formId && c.VenueCode == venueCode);
                if (candidateExamDetailsByForm.TryGetValue(formId, out var formExamDetails))
                {
                    UpdateCandidateExamDetailsInMemory(formExamDetails, candidateScores, paperType, paperSubtype);
                }

                // Get adaptive category scores from pre-fetched data (no DB query)
                Dictionary<(long CandidateId, long RegistrationId), Dictionary<long, decimal>> adaptiveCategoryScores = null;
                if (paperType == PaperType.Adaptive)
                {
                    adaptiveCategoryScoresByForm.TryGetValue(formId, out adaptiveCategoryScores);
                }

                // Build candidates list - SAME logic as ExportCandidatesData
                var candidates = metrics
                    .GroupBy(m => new { m.CandidateID, m.TrialNumber, m.RegistrationId })
                     .Where(g => questionLookup.ContainsKey((g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId)))
                    .Select(g =>
                    {
                        var key = (g.Key.CandidateID, g.Key.TrialNumber, g.Key.RegistrationId);
                        var q = questionLookup.GetValueOrDefault(key);

                        return new
                        {
                            g.Key.CandidateID,
                            g.Key.TrialNumber,
                            g.First().CandidateCode,
                            g.First().FirstName,
                            Gender = q?.Gender ?? false,
                            ClientCandidateID = q?.ClientCandidateID ?? "",
                            MiddleName = q?.MiddleName ?? "",
                            LastName = q?.LastName ?? ".",
                            RegistrationId = q?.RegistrationId ?? 0,
                            TCID = q?.TCID ?? 0,
                            ExamSeriesCode = q?.ExamSeriesCode ?? "",
                            FormName = q?.FormName ?? "",
                            ExamLanguage = q?.ExamLanguage ?? "",
                            ExamStartDate = q?.ExamStartDate,
                            PaperName = q?.PaperName ?? "",
                            PassingScore = q?.PassingScore ?? 0
                        };
                    })
                    .ToList();

                if (candidates.Count == 0) continue;

                // Get blocks for this form
                var blocks = blocksByForm.GetValueOrDefault(formId);

                // Generate DAT files - SAME methods as ExportCandidatesData
                var candidateDat = GenerateCandidateFile(candidates);
                var itemDat = GenerateItemFile(questions, paperType);
                var sectDat = GenerateSectionFile(questions, candidates, candidateScores, blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores);
                var examDat = GenerateExamFile(questions, candidates, candidateScores, paperType, paperSubtype, adaptiveCategoryScores);

                var examDate = questions.FirstOrDefault()?.ExamStartDate?.ToString("yyyy-MM-dd") ?? DateTimeHelper.Now.ToString("yyyy-MM-dd");

                var dateNow = DateTimeHelper.Now.ToString("yyyy-MM-dd");
                var zipBytes = CreateExportZip(
                    dateNow, candidates, questions, candidateScores,
                    blocks, categoryNames, paperType, paperSubtype, adaptiveCategoryScores
                );

                allResults.Add(new ExportCandidatesResponseDto
                {
                    FileName = $"Nemr-{examDate}.zip",
                    FileContent = Convert.ToBase64String(zipBytes),
                    ContentType = "application/zip",
                    CandidateCount = candidates.Count,
                    RegistrationIds = candidates.ConvertAll(c => (long)c.RegistrationId),
                    FormId = formId,
                    VenueId = venueCombo?.VenueId ?? 0,
                    VenueCode = venueCode,
                    PaperId = questions.FirstOrDefault()?.PaperId ?? 0,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ExamSeriesCode = questions.FirstOrDefault()?.ExamSeriesCode ?? "",
                    CandidateDat = candidateDat,
                    ItemDat = itemDat,
                    SectDat = sectDat,
                    ExamDat = examDat
                });
            }

            // Save all CandidateExamDetails updates in a single transaction
            await _commonService._unitOfWork.Complete();

            if (allResults.Count == 0)
            {
                _logger.LogWarning("=== ADVANCED MODE BATCH EXPORT ENDED (No Results) ===");
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesFound
                );
            }

            var totalCandidates = allResults.Sum(r => r.CandidateCount);
            var exportDate = DateTimeHelper.Now.ToString("yyyy-MM-dd");
            byte[] masterZipBytes;

            var mergedCandidateDat = string.Concat(allResults.Select(r => r.CandidateDat ?? ""));
            var mergedItemDat = string.Concat(allResults.Select(r => r.ItemDat ?? ""));
            var mergedSectDat = string.Concat(allResults.Select(r => r.SectDat ?? ""));
            var mergedExamDat = string.Concat(allResults.Select(r => r.ExamDat ?? ""));

            await using (var masterStream = new MemoryStream())
            {
                using (var masterZip = new ZipArchive(masterStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    async Task AddEntry(string name, string content)
                    {
                        var entry = masterZip.CreateEntry(name, CompressionLevel.Fastest);
                        await using var stream = entry.Open();
                        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content), cancellationToken);
                    }

                    await AddEntry($"cand-{exportDate}.dat", mergedCandidateDat);
                    await AddEntry($"item-{exportDate}.dat", mergedItemDat);
                    await AddEntry($"sect-{exportDate}.dat", mergedSectDat);
                    await AddEntry($"exam-{exportDate}.dat", mergedExamDat);
                }
                masterZipBytes = masterStream.ToArray();
            }

            _logger.LogInformation(
                "=== ADVANCED MODE BATCH EXPORT COMPLETED === Results: {ResultCount}, Candidates: {CandidateCount}",
                allResults.Count, totalCandidates);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.ExportCandidatesSuccessful, totalCandidates),
                allResults
            );
        }

        #endregion

        #region Advanced Mode - Database Update

        /// <summary>
        /// [ADVANCED MODE] Updates CandidateExamDetails in-memory with calculated scores.
        /// Uses pre-fetched data to avoid N+1 queries.
        /// IMPORTANT: Uses different FinalScore logic than Simple Mode:
        /// - Key format: CategoryName (string) vs CategoryId (Simple Mode)
        /// - FinalScore: Average of existing category scores vs Equation result (Simple Mode)
        /// </summary>
        private static void UpdateCandidateExamDetailsInMemory(
             List<CandidateExamDetails> candidateExamDetails,
             Dictionary<(long CandidateId, int Trial, long RegistrationId), CandidateScoreResult> candidateScores,
             PaperType paperType,
             AdaptivePaperSubtype paperSubtype)
        {
            foreach (var exam in candidateExamDetails)
            {
                var key = (exam.CandidateId, exam.TrialNumber, exam.RegistrationId);

                if (candidateScores.TryGetValue(key, out var scoreResult))
                {
                    exam.PaperType = paperType;

                    var generatedCategoryJson = scoreResult
                        .CategoryValues
                        .Select(c => new Dictionary<string, decimal>
                        {
                        { c.Key, c.Value.CalculatedValue }
                                })
                        .ToList();

                    var currentFinalScore = new List<Dictionary<string, decimal>>();

                    if (!string.IsNullOrWhiteSpace(exam.FinalScore))
                    {
                        try
                        {
                            currentFinalScore = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(
                                exam.FinalScore
                            ) ?? [];
                        }
                        catch
                        {
                            currentFinalScore = [];
                        }
                    }

                    currentFinalScore.RemoveAll(dict => dict.ContainsKey("FinalScore"));

                    var existingKeys = currentFinalScore
                        .SelectMany(dict => dict.Keys)
                        .ToHashSet();

                    var newItems = generatedCategoryJson
                        .ExceptBy(existingKeys, dict => dict.Keys.First())
                        .ToHashSet();

                    currentFinalScore.AddRange(newItems);

                    var scorePairs = currentFinalScore
                        .SelectMany(dict => dict)
                        .Where(kvp => long.TryParse(kvp.Key, out _))
                        .ToList();

                    decimal calculatedValue = (paperType == PaperType.Adaptive && scorePairs.Count > 0)
                        ? scorePairs.Average(kvp => kvp.Value)
                        : scoreResult.FinalScore;

                    if (paperSubtype == AdaptivePaperSubtype.STEP)
                    {
                        calculatedValue *= 100;
                    }

                    Dictionary<string, decimal> finalScore = new()
                    {
                        {
                            "FinalScore",
                            calculatedValue
                        }
                    };

                    currentFinalScore.Add(finalScore);

                    List<Dictionary<string, decimal>> finalScoreJson = currentFinalScore;

                    exam.FinalScore = JsonSerializer.Serialize(
                        finalScoreJson,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        }
                    );

                    exam.FScore = calculatedValue;
                    exam.GeneratedAt = DateTimeHelper.Now;
                }
            }
        }

        private static decimal ParseCalculatedFinalScore(string? finalScoreJson)
        {
            if (string.IsNullOrWhiteSpace(finalScoreJson)) return 0;

            try
            {
                var scoreList = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(finalScoreJson);
                if (scoreList == null) return 0;

                foreach (var dict in scoreList)
                {
                    if (dict.TryGetValue("FinalScore", out var value))
                        return value;
                }
                return 0;
            }
            catch { return 0; }
        }

        #endregion
    }
}
