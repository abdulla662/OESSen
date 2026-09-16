using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.MediaSetting.Requests;
using OES.Helper.Dtos.MediaSetting.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class MediaSettingService(ICommonService _commonService, FilterParamsValues _filterParamsValues) : IMediaSettingService
    {
        public async Task<ApiResponse> GetPaginatedMediaSettings(PaginationSearchModel pagination)
        {
            var organizationId = _filterParamsValues.OrganizationId;

            var query = _commonService._unitOfWork
                .Repository<MediaSetting, long>()
                .GetAll()
                .AsNoTracking()
                .Where(x => x.OrganizationId == 0 || x.OrganizationId == organizationId);

            // Filtering by SearchKey (MediaCategory name)
            if (!string.IsNullOrEmpty(pagination.SearchKey))
            {
                var matchingCategories = Enum.GetValues<MediaCategory>()
                    .Where(c => c.ToString().Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                query = query.Where(x => matchingCategories.Contains(x.MediaCategory));
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                         o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var paginatedData = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                           .Take(pagination.PageSize)
                                           .ToListAsync();

            var orgCustomCategories = await _commonService._unitOfWork.Repository<MediaSetting, long>()
                 .GetAll()
                 .AsNoTracking()
                 .Where(x => x.OrganizationId == organizationId)
                 .Select(x => x.MediaCategory)
                 .ToListAsync();

            var responseDtos = paginatedData.ConvertAll(ms =>
            {
                bool currentlyUsed = true;

                if (ms.OrganizationId == 0)
                {
                    currentlyUsed = !orgCustomCategories.Contains(ms.MediaCategory);
                }

                return new MediaSettingResponseDto(
                    ms.Id,
                    ms.MediaCategory,
                    ms.MaxSizeInKB,
                    ms.CreationUser,
                    ms.OrganizationId,
                    currentlyUsed
                );
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.DataRetrievedSuccessfully,
                new CustomTableData<MediaSettingResponseDto>(responseDtos, totalItems)
            );
        }

        public async Task<ApiResponse> GetByIdAsync(long id)
        {
            var mediaSetting = await _commonService
                ._unitOfWork
                .Repository<MediaSetting, long>()
                .GetByIdAsync(id);

            if (mediaSetting == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DataNotFound);
            }

            var response = new MediaSettingResponseDto(
                mediaSetting.Id,
                mediaSetting.MediaCategory,
                mediaSetting.MaxSizeInKB
            );

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.DataRetrievedSuccessfully,
                                response);
        }

        public async Task<ApiResponse> GetByMediaCategoryAsync(MediaCategory mediaCategory)
        {
            var organizationId = _filterParamsValues.OrganizationId;

            var settings = await _commonService
                ._unitOfWork
                .Repository<MediaSetting, long>()
                .GetAll()
                .AsNoTracking()
                .Where(x => x.MediaCategory == mediaCategory && (x.OrganizationId == 0 || x.OrganizationId == organizationId))
                .ToListAsync();

            var mediaSetting = settings.FirstOrDefault(x => x.OrganizationId == organizationId);

            if (mediaSetting == null)
            {
                mediaSetting = settings.FirstOrDefault(x => x.OrganizationId == 0);
            }

            if (mediaSetting == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DataNotFound);
            }

            var response = new MediaSettingResponseDto(
                mediaSetting.Id,
                mediaSetting.MediaCategory,
                mediaSetting.MaxSizeInKB,
                mediaSetting.CreationUser,
                mediaSetting.OrganizationId
            );

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.DataRetrievedSuccessfully,
                                response);
        }

        public async Task<ApiResponse> AddMediaAsync(MediaSettingRequestDto requestDto)
        {
            var organizationId = _filterParamsValues.OrganizationId;

            var existingSetting = await _commonService
                ._unitOfWork
                .Repository<MediaSetting, long>()
                .GetObjAsync(ms => ms.MediaCategory == requestDto.MediaCategory && ms.OrganizationId == organizationId);

            if (existingSetting != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.MediaCategoryAlreadyExists);
            }

            var newSetting = new MediaSetting(requestDto.MediaCategory, requestDto.MaxSizeInKB)
            {
                OrganizationId = organizationId,
                OrganizationSignature = _filterParamsValues.Signature
            };

            await _commonService._unitOfWork.Repository<MediaSetting, long>().AddAsync(newSetting);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var addedResponse = new MediaSettingResponseDto(
                    newSetting.Id,
                    newSetting.MediaCategory,
                    newSetting.MaxSizeInKB
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DataAddedSuccessfully,
                                    addedResponse);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToSaveData);
            }
        }

        public async Task<ApiResponse> UpdateMediaAsync(MediaSettingRequestDto requestDto)
        {
            var organizationId = _filterParamsValues.OrganizationId;

            var existingSetting = await _commonService
                ._unitOfWork
                .Repository<MediaSetting, long>()
                .GetObjAsync(ms => ms.MediaCategory == requestDto.MediaCategory && ms.OrganizationId == organizationId);

            if (existingSetting == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DataNotFound);
            }

            existingSetting.MaxSizeInKB = requestDto.MaxSizeInKB;

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                var updatedResponse = new MediaSettingResponseDto(
                    existingSetting.Id,
                    existingSetting.MediaCategory,
                    existingSetting.MaxSizeInKB
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DataUpdatedSuccessfully,
                                    updatedResponse);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToUpdateData);
            }
        }

        public async Task<ApiResponse> DeleteAsync(long id)
        {
            var mediaSetting = await _commonService
                ._unitOfWork
                .Repository<MediaSetting, long>()
                .GetObjAsync(ms => ms.Id == id);

            if (mediaSetting == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.DataNotFound);
            }

            _commonService._unitOfWork.Repository<MediaSetting, long>().SoftDelete(mediaSetting);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.DataDeletedSuccessfully);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToDeleteData);
            }
        }

        //public async Task<ApiResponse> GetUserMediaSettingsGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.MediaConfiguration) &&
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

        //public async Task<ApiResponse> GetMediaSettingsGroupsAsync(long mediaConfigurationId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<MediaSettingsGroups, long>()
        //        .GetAllAsync(
        //            x => x.MediaConfigurationId == mediaConfigurationId && !x.OESGroup.IsTemplate,
        //            Including: nameof(MediaSettingsGroups.OESGroup)
        //        );

        //    var dto = new MediaSettingsGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}
    }
}