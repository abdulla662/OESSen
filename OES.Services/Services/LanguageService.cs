using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;


        public LanguageService(ICommonService commonService, IMapper mapper, FilterParamsValues filterParamsValues)
        {
            _commonService = commonService;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
        }


        public async Task<ApiResponse> GetLanguageByIdAsync(long id)
        {
            var language = await _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetObjAsync(e => e.Id == id);

            if (language is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.LanguageNotFound
                );
            }

            var dto = _mapper.Map<GetLanguageDto>(language);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                dto
            );
        }


        public async Task<ApiResponse> GetAllLanguagesAsync()
        {
            var query = await _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetAllAsync();

            if (query == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.SomethingWentWrongTryAgainLater);
            }
            var mappedData = _mapper.Map<List<LanguageDto>>(query);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                mappedData);
        }


        public async Task<ApiResponse> GetAllQuestionDetailsLanguagesGroupAsync(long questionMetadataId)
        {
            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == questionMetadataId, Including: "QuestionDetails.Language");

            if (questionMetadataEntity == null || !questionMetadataEntity.QuestionDetails.Any())
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.SomethingWentWrongTryAgainLater);
            }

            var questionDetailsLanguagesGroup = questionMetadataEntity
                .QuestionDetails
                .Select(qd => new LanguageDto
                {
                    Id = qd.LanguageId,
                    Name = qd.Language.Name
                });

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                questionDetailsLanguagesGroup);
        }


        public async Task<ApiResponse> GetAllPaginatedLanguagesAsync(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetAll()
                .AsNoTracking();

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(x => x.CreationDate >= pagination.FromDate.Value);
            }

            if (pagination.ToDate.HasValue)
            {
                query = query.Where(x => x.CreationDate <= pagination.ToDate.Value);
            }

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                }

                query = pagination.OrderBy == SearchInKey.DESC ?
                    query.OrderByDescending(x => x.CreationDate) :
                    query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query
                    .Skip(pagination.PageIndex * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var mappedData = _mapper.Map<List<LanguageDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<LanguageDto>(mappedData, totalItems));
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                var mappedData = _mapper.Map<List<LanguageDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<LanguageDto>(mappedData, data.Count)
                );
            }
        }


        public async Task<ApiResponse> AddLanguageAsync(LanguageCreateDto languageCreateDto)
        {
            var trimmedName = languageCreateDto.Name.Trim().ToLower();

            var repo = _commonService._unitOfWork.Repository<Language, long>();

            bool nameExists = await repo.IsExistAsync(l => l.Name.Trim().ToLower() == trimmedName);

            if (nameExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.LanguageNameIsalreadyExists
                );
            }

            var entity = _mapper.Map<Language>(languageCreateDto);

            entity.Name = languageCreateDto.Name;
            entity.IsActive = true;

            await repo.AddAsync(entity);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Languageaddedsuccessfully
            );
        }


        public async Task<ApiResponse> UpdateLanguageAsync(LanguageUpdateDto languageUpdateDto)
        {
            var entity = await _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetByIdAsync(languageUpdateDto.Id);

            if (entity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.LanguageNotFound
                );
            }

            var trimmedName = languageUpdateDto.Name.Trim().ToLower();

            var repo = _commonService._unitOfWork.Repository<Language, long>();

            bool nameExists = await repo.IsExistAsync(l => l.Id != entity.Id && l.Name.Trim().ToLower() == trimmedName);

            if (nameExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.LanguageNameIsalreadyExists
                );
            }

            _mapper.Map(languageUpdateDto, entity);

            entity.Name = languageUpdateDto.Name;

            repo.Update(entity);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.LanguageUpdatedSuccessfully
            );
        }


        public async Task<ApiResponse> SoftDeleteLanguageAsync(long id)
        {
            var language = await _commonService
                ._unitOfWork
                .Repository<Language, long>()
                .GetByIdAsync(id);

            if (language == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.LanguageNotFound);
            }

            var checkingLanguageDependenciesResult = await CheckLanguageDependenciesAsync(language.Id);

            if (!string.IsNullOrWhiteSpace(checkingLanguageDependenciesResult))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkingLanguageDependenciesResult);
            }

            _commonService._unitOfWork
                .Repository<Language, long>()
                .SoftDelete(language);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.LanguageDeletedSuccessfully);
        }

        //public async Task<ApiResponse> GetUserLanguageGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Language) &&
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

        //public async Task<ApiResponse> GetLanguageGroupsAsync(long languageId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<LanguageGroups, long>()
        //        .GetAllAsync(
        //            x => x.LanguageId == languageId && !x.OESGroup.IsTemplate,
        //            Including: nameof(LanguageGroups.OESGroup)
        //        );

        //    var dto = new LanguageGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}

        #region Helper Methods
        private async Task<string> CheckLanguageDependenciesAsync(long languageId)
        {
            var hasAssociatedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .AnyAsync(qd => qd.LanguageId == languageId);

            if (hasAssociatedQuestions)
            {
                return Resource.LanguageCannotBeDeletedDueToQuestions;
            }

            var hasAssociatedPapers = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .Query()
                .AnyAsync(p => p.LanguageId == languageId);

            if (hasAssociatedPapers)
            {
                return Resource.LanguageCannotBeDeletedDueToPapers;
            }

            var hasAssociatedSchedules = await _commonService
                ._unitOfWork
                .Repository<ScheduleLanguage, long>()
                .Query()
                .AnyAsync(s => s.LanguageId == languageId);

            if (hasAssociatedSchedules)
            {
                return Resource.LanguageCannotBeDeletedDueToSchedules;
            }

            return null;
        }
        #endregion
    }
}
