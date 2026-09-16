using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class DeltaTypeService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : IDeltaTypeService
    {
        public async Task<IApiResponse> GetAllDeltaTypes()
        {
            var query = await _commonService._unitOfWork.Repository<DeltaType, long>().GetAllAsync();

            var mapData = _mapper.Map<List<GetDeltaTypeDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null,
                                                              mapData);
        }

        public async Task<IApiResponse> GetAllDeltaType(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<DeltaType, long>().GetAll().AsNoTracking();

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
                                         o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var data = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                  .Take(pagination.PageSize)
                                  .ToListAsync();

            var mappedData = _mapper.Map<List<GetDeltaTypeDto>>(data);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                new CustomTableData<GetDeltaTypeDto>(mappedData, totalItems)
                );
        }

        public async Task<IApiResponse> GetDeltaTypeById(long id)
        {
            var DeltaType = await _commonService._unitOfWork.Repository<DeltaType, long>().GetObjAsync(x => x.Id == id);

            if (DeltaType is not null)
            {
                var mapDelta = _mapper.Map<GetDeltaTypeDto>(DeltaType);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  $"Delta Type With ID {id} get Successfully",
                                                                  mapDelta);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                              HttpStatusCode.NotFound,
                                                              $"Delta Type With ID {id} Not found");
        }

        public async Task<IApiResponse> AddDeltaType(AddDeltaTypeDto addDeltaTypeDto)
        {
            var mapped = _mapper.Map<DeltaType>(addDeltaTypeDto);

            await _commonService._unitOfWork.Repository<DeltaType, long>().AddAsync(mapped);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.CreatedDeltaType,
                                                                  mapped);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  Resource.SaveRecordError);
            }
        }

        public async Task<IApiResponse> UpdateDeltaType(GetDeltaTypeDto updateDeltaType)
        {
            var deltaType = await _commonService._unitOfWork.Repository<DeltaType, long>().GetObjAsync(x => x.Id == updateDeltaType.Id);

            if (deltaType is not null)
            {
                _mapper.Map(updateDeltaType, deltaType);

                _commonService._unitOfWork.Repository<DeltaType, long>().Update(deltaType);

                await _commonService._unitOfWork.Complete();

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.UpdatedDeltaType,
                                                                  deltaType);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  "Unable to save this record please try again later");
            }
        }

        public async Task<IApiResponse> SoftDeleteDeltaType(long id)
        {
            var deltaType = await _commonService._unitOfWork.Repository<DeltaType, long>().GetObjAsync(x => x.Id == id);

            if (deltaType is not null)
            {
                _commonService._unitOfWork.Repository<DeltaType, long>().SoftDelete(deltaType);

                await _commonService._unitOfWork.Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.DeletedDeltaType,
                                        deltaType);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        $"Delta Type with ID {id} not found.");
            }
        }

        //public async Task<ApiResponse> GetUserDeltaTypeGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.DeltaType) &&
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

        //public async Task<ApiResponse> GetDeltaTypeGroupsAsync(long deltaTypeId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<DeltaTypeGroups, long>()
        //        .GetAllAsync(
        //            x => x.DeltaTypeId == deltaTypeId && !x.OESGroup.IsTemplate,
        //            Including: nameof(DeltaTypeGroups.OESGroup)
        //        );

        //    var dto = new DeltaTypeGroupDto();

        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));

        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}
    }
}
