using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class DifficultyLevelService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : IDifficultyLevelService
    {
        public async Task<IApiResponse> GetAllDifficultyLevel(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .Query()
                .Include(e => e.DifficultyProfile)
                .Include(e => e.DeltaType)
                .AsNoTracking()
                .AsQueryable();

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    query = query.Where(dl => dl.Name.Contains(pagination.SearchKey) ||
                                              dl.DifficultyProfile.Name.Contains(pagination.SearchKey) ||
                                              dl.DifficultyProfile.Description.Contains(pagination.SearchKey));
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate && o.CreationDate < (pagination.ToDate ?? DateTime.Today).Date.AddDays(1));
                }

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = _mapper.Map<List<DifficultyLevelDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationIsOn,
                    new CustomTableData<DifficultyLevelDto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await query.ToListAsync();

                var mappedData = _mapper.Map<List<DifficultyLevelDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationIsOff,
                    new CustomTableData<DifficultyLevelDto>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> GetAllDifficultyLevels()
        {
            var difficultyLevels = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetAllAsync(
                    Including: nameof(DifficultyLevel.DifficultyProfile),
                    asNoTracking: true
                );

            var MappedData = _mapper.Map<List<DifficultyLevelDto>>(difficultyLevels);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.DifficultyLevelRetrievedSuccessfully,
                                MappedData);
        }

        public async Task<ApiResponse> GetAllDifficultyLevelsByProfileIdAsync(long? profileId)
        {
            if (profileId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.ProfileIdMustBePositiveNumber);
            }

            var difficultyLevels = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetAllAsync(x => x.DifficultyProfileId == profileId, null, Including: "DifficultyProfile,DeltaType");

            if (difficultyLevels == null || !difficultyLevels.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                                  HttpStatusCode.NotFound,
                                                                  string.Format(Resource.NoDifficultyLevelsFoundForProfileId, profileId));
            }

            var MappedData = _mapper.Map<List<DifficultyLevelDto>>(difficultyLevels);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.DifficultyLevelRetrievedSuccessfully,
                                MappedData);
        }

        public async Task<IApiResponse> GetDifficultyLevelsByDeltaTypeId(long DeltaTypeId)
        {
            var query = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetAllAsync(x => x.DeltaTypeId == DeltaTypeId);

            var mapData = _mapper.Map<List<DifficultyLevelDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              mapData);
        }

        public async Task<ApiResponse> GetDifficultyLevelWithQuestionCountByProfileIdAsync(long? profileId)
        {
            var difficultyLevels = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetAllAsync(x => x.DifficultyProfileId == profileId);

            var questions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(x => difficultyLevels.Select(x => x.Id)
                .Contains(x.DifficultyLevelId));

            var questionCountByDifficultyLevel = questions
                .GroupBy(q => q.DifficultyLevelId)
                .Select(g => new
                {
                    DifficultyLevelId = g.Key,
                    QuestionCount = g.Count()
                })
                .ToList();

            var result = difficultyLevels.Select(x => new GetDifficultyLevelWithQuestionCountByProfileIdDto
            {
                DifficultyLevelId = x.Id,
                DifficultyLevelName = x.Name,
                QuestionCount = questionCountByDifficultyLevel
                                       .FirstOrDefault(qc => qc.DifficultyLevelId == x.Id)?.QuestionCount ?? 0
            }).ToList();

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              result);
        }

        public async Task<ApiResponse> GetDifficultyLevelByIdAsync(long id)
        {
            var difficultyLevel = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetObjAsync(e => e.Id == id, Including: "DifficultyProfile,DeltaType");

            if (difficultyLevel == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DifficultyLevelNotFound);
            }

            DifficultyLevelDto difficultyLevelDto = new()
            {
                Id = difficultyLevel.Id,
                Name = difficultyLevel.Name,
                FromDelta = difficultyLevel.FromDelta,
                ToDelta = difficultyLevel.ToDelta,
                DifficultyProfileName = difficultyLevel.DifficultyProfile.Name,
                DifficultyProfileDescription = difficultyLevel.DifficultyProfile.Description,
                DifficultyProfileId = difficultyLevel.DifficultyProfileId,
                DeltaTypeId = difficultyLevel.DeltaTypeId,
                DeltaTypeName = difficultyLevel.DeltaType.Name
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.DifficultyLevelRetrievedSuccessfully,
                                difficultyLevelDto);
        }

        public async Task<ApiResponse> AddDifficultyLevelAsync(AddDifficultyLevelDto levelDto)
        {
            var includes = $"{nameof(DifficultyProfile.DifficultyLevels)},{nameof(DifficultyProfile.PaperMetadata)}";

            var difficultyProfile = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(e => e.Id == levelDto.DifficultyProfileId, includes);

            var checkingProfileDependenciesResult = await CheckDifficultyProfileDependenciesAsync(difficultyProfile);

            if (!string.IsNullOrWhiteSpace(checkingProfileDependenciesResult))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkingProfileDependenciesResult);
            }

            var similarDifficultyLevelNameExists = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .IsExistAsync(x => x.Name.ToLower() == levelDto.Name.ToLower() && x.OrganizationId == levelDto.OrganizationId && x.DifficultyProfileId == levelDto.DifficultyProfileId);

            if (similarDifficultyLevelNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.DifficultyLevelNameIsExist);
            }

            var query = _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetAll(x => x.DifficultyProfileId == levelDto.DifficultyProfileId && x.DeltaTypeId == levelDto.DeltaTypeId);

            if (IsCommon(levelDto.DeltaTypeId, query))
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.BadRequest,
                                            Resource.CanotAssignTwoCommonTypesToTheSameProfile);
            }
            else if (HasDifficultyLevelOverlap(query, levelDto.FromDelta, levelDto.ToDelta))
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.BadRequest,
                                            Resource.FromDeltaCanNotBeInThisRange);
            }

            var mappedLevel = _mapper.Map<DifficultyLevel>(levelDto);

            await _commonService._unitOfWork.Repository<DifficultyLevel, long>().AddAsync(mappedLevel);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            Resource.DifficultyLevelAddedSuccessfully);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            Resource.FailToSaveThisDifficultyLevel);
            }
        }

        public async Task<ApiResponse> UpdateDifficultyLevelAsync(UpdateDifficultyLevelDto updateDto)
        {
            var similarNameExists = await _commonService
               ._unitOfWork
               .Repository<DifficultyLevel, long>()
               .IsExistAsync(e => e.Name.ToLower() == updateDto.Name.ToLower() && e.DifficultyProfileId.Equals(updateDto.DifficultyProfileId) && e.Id != updateDto.Id);

            if (similarNameExists)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.AlreadyExist, HttpStatusCode.Conflict, Resource.FailToUpdateThisDifficultyLevel);

            var includes = $"{nameof(DifficultyProfile.DifficultyLevels)},{nameof(DifficultyProfile.PaperMetadata)}";

            var difficultyProfile = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(e => e.Id == updateDto.DifficultyProfileId, includes);

            var checkingProfileDependenciesResult = await CheckDifficultyProfileDependenciesAsync(difficultyProfile);

            if (!string.IsNullOrWhiteSpace(checkingProfileDependenciesResult))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkingProfileDependenciesResult);
            }

            var existingLevel = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetObjAsync(e => e.Id == updateDto.Id);

            if (existingLevel == null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.DifficultyLevelNotFound);
            }

            var query = _commonService
                        ._unitOfWork
                        .Repository<DifficultyLevel, long>()
                        .GetAll(x => x.DifficultyProfileId == updateDto.DifficultyProfileId &&
                                x.DeltaTypeId == updateDto.DeltaTypeId &&
                                x.Id != updateDto.Id);

            if (IsCommon(updateDto.DeltaTypeId, query))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.BadRequest,
                                        Resource.CanotAssignTwoCommonTypesToTheSameProfile);
            }
            else if (HasDifficultyLevelOverlap(query, updateDto.FromDelta, updateDto.ToDelta))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.BadRequest,
                                        Resource.FromDeltaCanNotBeInThisRange);
            }

            _mapper.Map(updateDto, existingLevel);

            _commonService._unitOfWork.Repository<DifficultyLevel, long>().Update(existingLevel);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DifficultyLevelUpdatedSuccessfully);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailToUpdateThisDifficultyLevel);
            }
        }

        public async Task<ApiResponse> DeleteDifficultyLevelAsync(long id)
        {
            var difficultyLevel = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetObjAsync(e => e.Id == id);

            if (difficultyLevel == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DifficultyLevelNotFound);
            }

            var includes = $"{nameof(DifficultyProfile.DifficultyLevels)},{nameof(DifficultyProfile.PaperMetadata)}";

            var difficultyProfile = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(e => e.Id == difficultyLevel.DifficultyProfileId, includes);

            var checkingProfileDependenciesResult = await CheckDifficultyProfileDependenciesAsync(difficultyProfile);

            if (!string.IsNullOrWhiteSpace(checkingProfileDependenciesResult))
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkingProfileDependenciesResult);
            }

            _commonService._unitOfWork.Repository<DifficultyLevel, long>().SoftDelete(difficultyLevel);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DifficultyLevelDeletedSuccessfully);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailTodeleteThisDifficultyLevel);
            }
        }

        //public async Task<ApiResponse> GetUserDifficultyLevelGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.DifficultyLevel) &&
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

        //public async Task<ApiResponse> GetDifficultyLevelGroupsAsync(long difficultyLevelId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<DifficultyLevelGroups, long>()
        //        .GetAllAsync(
        //            x => x.DifficultyLevelId == difficultyLevelId && !x.OESGroup.IsTemplate,
        //            Including: nameof(DifficultyLevelGroups.OESGroup)
        //        );

        //    var dto = new DifficultyLevelGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}

        #region Helper Methods
        public static bool HasDifficultyLevelOverlap(IQueryable<DifficultyLevel> difficultyLevels, decimal fromDelta, decimal UpperDScore)
        {
            return difficultyLevels.Any(d => d.ToDelta >= fromDelta && d.FromDelta <= UpperDScore);
        }

        public bool IsCommon(long deltaTypeId, IQueryable<DifficultyLevel> difficultyLevels)
        {
            var commonID = _commonService
                ._unitOfWork
                .Repository<DeltaType, long>()
                .Query(false, false)
                .ToList()
                .First(x => x.Name.Equals(DeltaTypes.Common.ToString(), StringComparison.CurrentCultureIgnoreCase)).Id;

            return deltaTypeId == commonID && difficultyLevels.Any(x => x.DeltaTypeId == commonID);
        }

        public async Task<string> CheckDifficultyProfileDependenciesAsync(DifficultyProfile difficultyProfile)
        {
            if (difficultyProfile.CreationUser == DefaultSystemUser.Name)
            {
                return Resource.DefaultDifficultyProfileCannotBeModified;
            }

            var hasActiveQuestionMetaData = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(q => q.DifficultyProfileId == difficultyProfile.Id && !q.IsDeleted);

            if (hasActiveQuestionMetaData)
            {
                return Resource.DifficultyProfileCannotBeModifiedDueToQuestions;
            }

            var hasActivePaperMetdata = difficultyProfile.PaperMetadata.Any(pm => !pm.IsDeleted);

            if (hasActivePaperMetdata)
            {
                return Resource.DifficultyProfileCannotBeModifiedDueToPapers;
            }

            return null;
        }
        #endregion
    }
}
