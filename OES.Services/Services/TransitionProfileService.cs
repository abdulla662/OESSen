using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class TransitionProfileService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : ITransitionProfileService
    {
        public async Task<IApiResponse> AddTransitionProfile(AddTransitionProfileDto _addProfile)
        {
            _addProfile.Name = _addProfile.Name.Trim();

            var similarProfileExists = await _commonService
                ._unitOfWork
                .Repository<TransitionProfile, long>()
                .IsExistAsync(n => n.Name.ToLower() == _addProfile.Name.ToLower());

            if (similarProfileExists)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        Resource.TransitionProfileWithThisnameisAlreadyExists);
            }

            var mapData = _mapper.Map<TransitionProfile>(_addProfile);

            await _commonService._unitOfWork.Repository<TransitionProfile, long>().AddAsync(mapData);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                                    ._apiResponse
                                    .GetApiResponse(CustomCodeStatus.Success,
                                                    HttpStatusCode.OK,
                                                    Resource.TransitionProfileAddedSuccessfully,
                                                    mapData);
            }
            else
            {
                return _commonService
                                    ._apiResponse
                                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                    HttpStatusCode.InternalServerError,
                                                    Resource.FailToSaveTransitionProfile);
            }
        }


        public async Task<IApiResponse> GetAllTransitionProfile(PaginationSearchModel searchModel)
        {
            var query = _commonService._unitOfWork.Repository<TransitionProfile, long>().GetAll().AsNoTracking();

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
                                    o.CreationDate < (searchModel.ToDate ?? DateTime.Today).AddDays(1));
            }

            query = searchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var countItems = await query.CountAsync();

            var data = await query.Skip(searchModel.PageIndex * searchModel.PageSize)
                                  .Take(searchModel.PageSize)
                                  .ToListAsync();

            var mapData = _mapper.Map<List<TransitionProfileDto>>(data);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              new CustomTableData<TransitionProfileDto>(mapData, countItems));
        }


        public async Task<IApiResponse> EditTransitionProfileAsync(TransitionProfileUpdateRequestDto transitionProfileUpdateRequestDto)
        {
            transitionProfileUpdateRequestDto.Name = transitionProfileUpdateRequestDto.Name.Trim();

            var entity = await _commonService
                ._unitOfWork
                .Repository<TransitionProfile, long>()
                .GetByIdAsync(transitionProfileUpdateRequestDto.Id);

            if (entity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TransitionProfileNotFound
                );
            }

            if (entity.Name != transitionProfileUpdateRequestDto.Name)
            {
                bool similarProfileExists = await _commonService
                    ._unitOfWork
                    .Repository<TransitionProfile, long>()
                    .IsExistAsync(l => l.Name.ToLower() == transitionProfileUpdateRequestDto.Name.ToLower() && l.Id != entity.Id);

                if (similarProfileExists)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.ValidationError,
                        HttpStatusCode.BadRequest,
                        Resource.TransitionProfileWithThisnameisAlreadyExists);
                }
            }

            entity.Name = transitionProfileUpdateRequestDto.Name;
            entity.Description = transitionProfileUpdateRequestDto.Description;
            entity.DifficultyProfileId = transitionProfileUpdateRequestDto.DifficultyProfileId;

            _commonService
                ._unitOfWork
                .Repository<TransitionProfile, long>()
                .Update(entity);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Transitionprofileupdatedsuccessfully);
        }


        public async Task<IApiResponse> GetProfiles()
        {
            var query = _commonService._unitOfWork.Repository<TransitionProfile, long>().GetAll();

            var mapData = _mapper.Map<List<TransitionProfileDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              mapData);
        }


        public async Task<IApiResponse> GetTransitionProfileById(long id)
        {
            var transitionProfile = await _commonService
                ._unitOfWork
                .Repository<TransitionProfile, long>()
                .GetObjAsync(e => e.Id == id);

            if (transitionProfile == null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.TransitionProfileNotFound);
            }

            var mapData = _mapper.Map<TransitionProfileDto>(transitionProfile);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              mapData);
        }

        //public async Task<ApiResponse> GetTransitionProfileGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Transition) &&
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

        //public async Task<ApiResponse> GetTransitionGroupsAsync(long TransitionProfileId)
        //{
        //    var equationGroups = await _commonService
        //        ._unitOfWork
        //        .Repository<TransitionProfileGroups, long>()
        //        .GetAllAsync(
        //            x => x.ProfileId == TransitionProfileId &&
        //                 !x.OESGroup.IsTemplate,
        //            Including: nameof(TransitionProfileGroups.OESGroup)
        //        );

        //    var transitionGroupsDto = new TransitionProfileDto();

        //    equationGroups.ToList().ForEach(ibg => transitionGroupsDto.GroupsIds.Add(ibg.OESGroupId));

        //    transitionGroupsDto.OwnerGroupId = equationGroups
        //        .FirstOrDefault(x =>
        //            x.OESGroup != null &&
        //            x.OESGroup.AutoCreatedForUser
        //        )?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        transitionGroupsDto
        //    );
        //}

        public async Task<IApiResponse> SoftDeleteTransitionProfileAsync(long profileId)
        {
            var transitionProfile = await _commonService
                ._unitOfWork
                .Repository<TransitionProfile, long>()
                .GetObjAsync(e => e.Id == profileId, Including: $"{nameof(TransitionProfile.TransitionLevels)}");

            var isPaperExists = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .IsExistAsync(pm => pm.TransitionProfileId == profileId);

            if (isPaperExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.TransitionProfileUsedInPaper);
            }

            if (transitionProfile == null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.TransitionProfileNotFound);
            }

            foreach (var transitionLevel in transitionProfile.TransitionLevels)
            {
                _commonService._unitOfWork.Repository<TransitionLevel, long>().SoftDelete(transitionLevel);
            }

            _commonService._unitOfWork.Repository<TransitionProfile, long>().SoftDelete(transitionProfile);

            await _commonService._unitOfWork.Complete();

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.TransitionProfileHasbeenDeletedSuccessfully,
                                    transitionProfile);
        }
    }
}
