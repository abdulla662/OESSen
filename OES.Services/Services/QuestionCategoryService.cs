using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionCategoryService(ICommonService _commonService, FilterParamsValues _filterParamsValues) : IQuestionCategoryService
    {
        public async Task<IApiResponse> GetAllCategory(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<QuestionCategory, long>().GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey))
            {
                if (pagination.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                }
            }

            if (pagination.FromDate is not null)
            {
                query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                    o.CreationDate < (pagination.ToDate ?? DateTime.Today).AddDays(1));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var data = await query
                .Skip(pagination.PageIndex * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var mappedData = _commonService._mapper.Map<List<QuestionCategoryDto>>(data);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                "Pagination Is On",
                                new CustomTableData<QuestionCategoryDto>(mappedData, totalItems)
                );
        }

        public async Task<IApiResponse> GetCategories()
        {
            var category = _commonService._unitOfWork.Repository<QuestionCategory, long>().GetAll();

            var mapCategories = _commonService._mapper.Map<List<QuestionCategoryDto>>(category);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              "Get All Categories Successfully",
                                                              mapCategories);
        }

        public async Task<IApiResponse> GetCategoryById(long id)
        {
            var category = await _commonService._unitOfWork.Repository<QuestionCategory, long>().GetObjAsync(x => x.Id == id);

            if (category is not null)
            {
                var mapCategory = _commonService._mapper.Map<QuestionCategoryDto>(category);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  $"Category With ID {id} get Successfully",
                                                                  mapCategory);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                              HttpStatusCode.NotFound,
                                                              $"Category With ID {id} Not found");
        }

        public async Task<IApiResponse> AddCategory(AddQuestionCategoryDto _addCategory)
        {
            var existingCategory = await _commonService._unitOfWork
                .Repository<QuestionCategory, long>()
                .GetObjAsync(c => c.Name.Trim().ToLower() == _addCategory.Name.Trim().ToLower());

            if (existingCategory != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.CategoryNameAlreadyExists
                );
            }

            var mapCategory = _commonService._mapper.Map<QuestionCategory>(_addCategory);

            await _commonService._unitOfWork.Repository<QuestionCategory, long>().AddAsync(mapCategory);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.QuestionCategoryAddedSuccessfully,
                                                                  mapCategory);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  "Unable to save this record please try again later");
            }
        }

        public async Task<IApiResponse> UpdateCategory(QuestionCategoryDto _updateCategory)
        {
            var existingCategory = await _commonService._unitOfWork
                .Repository<QuestionCategory, long>()
                .GetObjAsync(c => c.Id != _updateCategory.Id && c.Name.Trim().ToLower() == _updateCategory.Name.Trim().ToLower());

            if (existingCategory != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.CategoryNameAlreadyExists
                );
            }

            var category = await _commonService._unitOfWork.Repository<QuestionCategory, long>().GetObjAsync(x => x.Id == _updateCategory.Id);

            if (category is not null)
            {
                _commonService._mapper.Map(_updateCategory, category);

                _commonService._unitOfWork.Repository<QuestionCategory, long>().Update(category);

                await _commonService._unitOfWork.Complete();

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.QuestionCategoryUpdatedSuccessfully,
                                                                  category);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  "Unable to save this record please try again later");
            }
        }

        public async Task<IApiResponse> SoftDeleteCategory(long id)
        {
            var category = await _commonService._unitOfWork.Repository<QuestionCategory, long>().GetObjAsync(x => x.Id == id);

            if (category is not null)
            {
                var checkingCategoryDependenciesResult = await CheckCategoryDependenciesAsync(category.Id);

                if (!string.IsNullOrWhiteSpace(checkingCategoryDependenciesResult))
                {
                    return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Conflict,
                                            HttpStatusCode.Conflict,
                                            checkingCategoryDependenciesResult);
                }

                _commonService._unitOfWork.Repository<QuestionCategory, long>().SoftDelete(category);

                await _commonService._unitOfWork.Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.QuestionCategoryHasbeenDeleted,
                                        category);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    $"Question Category with ID {id} not found.");
            }
        }

        public async Task<IApiResponse> BulkUpdateCategoryFinalScoresAsync(List<QuestionCategoryDto> categoriesWithCustomNames)
        {
            var allCategories = await _commonService
                ._unitOfWork
                .Repository<QuestionCategory, long>()
                .GetAll()
                .ToListAsync();

            var customNamesDict = categoriesWithCustomNames.ToDictionary(c => c.Id, c => c.FinalScoreName);

            foreach (var category in allCategories)
            {
                if (customNamesDict.TryGetValue(category.Id, out var customName) && !string.IsNullOrWhiteSpace(customName))
                {
                    category.FinalScoreName = customName.Trim();
                }
                else if (string.IsNullOrWhiteSpace(category.FinalScoreName))
                {
                    category.FinalScoreName = category.Name;
                }
            }

            _commonService._unitOfWork.Repository<QuestionCategory, long>().UpdateRange(allCategories);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionCategoryUpdatedSuccessfully,
                allCategories
            );
        }

        public async Task<IApiResponse> GetCategoriesByPaperId(long id)
        {
            var paperStageCategories = await _commonService
                ._unitOfWork
                .Repository<PaperStageCategoryDecisionPath, long>()
                .GetAllAsync(x => x.PaperId == id);

            if (paperStageCategories?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCategoriesFound
                );
            }

            var categoryIds = paperStageCategories
                .Select(x => x.QuestionCategoryId)
                .Distinct()
                .ToList();

            var categories = await _commonService
                ._unitOfWork
                .Repository<QuestionCategory, long>()
                .GetAllAsync(x => categoryIds.Contains(x.Id));

            if (categories?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCategoriesFound
                );
            }

            var mapCategories = _commonService._mapper.Map<List<QuestionCategoryDto>>(categories);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CategoriesRetrievedSuccessfully,
                mapCategories
            );
        }

        //public async Task<IApiResponse> GetUserQuestionCategoryGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.QuestionCategory) &&
        //        (
        //            g.CreationUser.ToLower() == currentUser ||
        //            g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
        //        ),
        //        Including: nameof(OESGroup.GroupResources)
        //    );

        //    var groupDtos = groups
        //        .Select(g => new GetOESGroupDto { Id = g.Id, Name = g.Name })
        //        .DistinctBy(x => x.Id)
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, groupDtos);
        //}

        //public async Task<IApiResponse> GetQuestionCategoryGroupsAsync(long questionCategoryId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<QuestionCategoryGroups, long>()
        //        .GetAllAsync(
        //            x => x.QuestionCategoryId == questionCategoryId && !x.OESGroup.IsTemplate,
        //            Including: nameof(QuestionCategoryGroups.OESGroup)
        //        );

        //    var dto = new QuestionCategoryGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}


        #region Helper Methods

        private async Task<string> CheckCategoryDependenciesAsync(long categoryId)
        {
            var hasAssociatedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AnyAsync(q => q.QuestionCategoryId == categoryId);

            if (hasAssociatedQuestions)
            {
                return Resource.CategoryCannotBeDeletedDueToQuestions;
            }

            var hasAssociatedBlocks = await _commonService
                ._unitOfWork
                .Repository<Block, long>()
                .Query()
                .AnyAsync(q => q.QuestionCategoryId == categoryId);

            if (hasAssociatedBlocks)
            {
                return Resource.CategoryCannotBeDeletedDueToBlocks;
            }

            return null;
        }

        #endregion Helper Methods
    }
}