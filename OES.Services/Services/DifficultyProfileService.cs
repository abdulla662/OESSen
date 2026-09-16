using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class DifficultyProfileService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : IDifficultyProfileService
    {
        public async Task<IApiResponse> GetAllDifficultyProfile(PaginationSearchModel searchModel)
        {
            var query = _commonService._unitOfWork.Repository<DifficultyProfile, long>().GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(searchModel.SearchKey))
            {
                if (searchModel.SearchInName && searchModel.SearchInDescription)
                {
                    query = query.Where(x => x.Name.Contains(searchModel.SearchKey) || x.Description.Contains(searchModel.SearchKey));
                }
                else if (searchModel.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(searchModel.SearchKey));
                }
                else if (searchModel.SearchInDescription)
                {
                    query = query.Where(x => x.Description.Contains(searchModel.SearchKey));
                }
            }

            if (searchModel.FromDate is not null)
            {
                query = query.Where(o => o.CreationDate >= searchModel.FromDate &&
                                         o.CreationDate <= (searchModel.ToDate ?? DateTimeHelper.Now.Date));
            }

            query = searchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var countItems = await query.CountAsync();

            var data = await query
                .Skip(searchModel.PageIndex * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .ToListAsync();

            var mapData = _mapper.Map<List<DifficultyProfileDto>>(data);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              "Pagination Is On",
                                                              new CustomTableData<DifficultyProfileDto>(mapData, countItems));

        }

        public async Task<IApiResponse> GetProfiles()
        {
            var query = _commonService._unitOfWork.Repository<DifficultyProfile, long>().GetAll();

            var mapData = _mapper.Map<List<ProfileDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              "Get All Profile Successfully",
                                                              mapData);
        }

        public async Task<IApiResponse> GetProfileById(long id)
        {
            var profile = await _commonService._unitOfWork.Repository<DifficultyProfile, long>().GetObjAsync(e => e.Id == id);

            if (profile is not null)
            {
                var data = _mapper.Map<DifficultyProfileDto>(profile);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null,
                                                                  data);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                              HttpStatusCode.NotFound,
                                                              "Profile not found",
                                                              null);
        }

        public async Task<IApiResponse> AddDifficultyProfile(AddDifficultyProfileDto _addDifficultyProfile)
        {
            var similarNameExists = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .IsExistAsync(x => x.Name.ToLower() == _addDifficultyProfile.Name.ToLower());

            if (similarNameExists)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ValidationError,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.DifficultyProfileNameisAlreadyExists);
            }

            var mapData = _mapper.Map<DifficultyProfile>(_addDifficultyProfile);

            await _commonService._unitOfWork.Repository<DifficultyProfile, long>().AddAsync(mapData);

            if (await _commonService._unitOfWork.Complete() > 0)
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.DifficultyProfileAddedSuccessfully,
                                        mapData);
            else
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.FailToSaveDifficultyProfile);
        }

        public async Task<IApiResponse> UpdateDifficultyProfile(DifficultyProfileDto _updateDifficultyProfile)
        {
            var difficultyProfile = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(e => e.Id == _updateDifficultyProfile.Id, Including: $"{nameof(DifficultyProfile.PaperMetadata)}");

            if (_updateDifficultyProfile.Name != difficultyProfile.Name)
            {
                var nameExists = await _commonService
                    ._unitOfWork
                    .Repository<DifficultyProfile, long>()
                    .IsExistAsync(dp => dp.Id != difficultyProfile.Id && dp.Name.ToLower() == _updateDifficultyProfile.Name.ToLower());

                if (nameExists)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.ValidationError,
                        HttpStatusCode.BadRequest,
                        Resource.DifficultyProfileNameisAlreadyExists
                    );
                }
            }

            _mapper.Map(_updateDifficultyProfile, difficultyProfile);

            _commonService._unitOfWork.Repository<DifficultyProfile, long>().Update(difficultyProfile);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            Resource.DifficultyProfileUpdatedSuccessfully,
                                            difficultyProfile);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            Resource.FailtoUpdateDifficultyProfile);
            }
        }

        public async Task<IApiResponse> SoftDeleteDifficultyProfileAsync(long id)
        {
            var includes = $"{nameof(DifficultyProfile.DifficultyLevels)},{nameof(DifficultyProfile.PaperMetadata)}";

            var difficultyProfile = await _commonService
                ._unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(e => e.Id == id, includes);

            if (difficultyProfile == null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.DifficultyProfileNotFound);
            }

            var checkMetadataValidator = await CheckDifficultyProfileDependenciesAsync(id, difficultyProfile.PaperMetadata);

            if (checkMetadataValidator != null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkMetadataValidator);
            }

            foreach (var difficultyLevel in difficultyProfile.DifficultyLevels)
            {
                _commonService._unitOfWork.Repository<DifficultyLevel, long>().SoftDelete(difficultyLevel);

            }

            _commonService._unitOfWork.Repository<DifficultyProfile, long>().SoftDelete(difficultyProfile);

            await _commonService._unitOfWork.Complete();

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DifficultyProfileHasbeenDeletedSuccessfully,
                                    difficultyProfile);
        }

        //public async Task<ApiResponse> GetUserDifficultyProfileGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.DifficultyProfile) &&
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

        //public async Task<ApiResponse> GetDifficultyProfileGroupsAsync(long difficultyProfileId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<DifficultyProfileGroups, long>()
        //        .GetAllAsync(
        //            x => x.DifficultyProfileId == difficultyProfileId && !x.OESGroup.IsTemplate,
        //            Including: nameof(DifficultyProfileGroups.OESGroup)
        //        );

        //    var dto = new DifficultyProfileGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}

        #region Helper Methods

        public async Task<string> CheckDifficultyProfileDependenciesAsync(long difficultyProfileid, ICollection<PaperMetadata> paperMetadata)
        {
            var hasActiveQuestionMetaData = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .IsExistAsync(q => q.DifficultyProfileId == difficultyProfileid && !q.IsDeleted);

            if (hasActiveQuestionMetaData)
            {
                return Resource.DifficultyProfileCannotBeModifiedDueToQuestions;
            }

            var hasActivePaperMetdata = paperMetadata.Any(pm => !pm.IsDeleted);

            if (hasActivePaperMetdata)
            {
                return Resource.DifficultyProfileCannotBeModifiedDueToPapers;
            }

            return null;
        }

        #endregion
    }
}
