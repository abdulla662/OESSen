using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.OESRole;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using SharedHelper.General;
using System.Linq.Expressions;
using System.Net;

namespace OES.Services.Services
{
    public class AppGroupService : IAppGroupService
    {
        private readonly IAppGroupValidatorService _appGroupValidatorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiResponse _apiResponse;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _FilterParamssValues;

        public AppGroupService(IAppGroupValidatorService appGroupValidatorService,
                               IUnitOfWork unitOfWork,
                               IApiResponse apiResponse,
                               IMapper mapper,
                               FilterParamsValues filterParamssValues)
        {
            _appGroupValidatorService = appGroupValidatorService;
            _unitOfWork = unitOfWork;
            _apiResponse = apiResponse;
            _mapper = mapper;
            _FilterParamssValues = filterParamssValues;
        }

        public async Task<IApiResponse> GetPaginatedGroupsAsync(PaginationSearchModel pagination)
        {
            var query = _unitOfWork
                .Repository<OESGroup, Guid>()
                .GetAll()
                .AsNoTracking();

            // Apply pagination and filters if necessary
            if (!pagination.PaginationOff)
            {
                // Apply search filter if search key is provided
                if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                }

                // Apply date filters if provided
                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                             o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                // Apply ordering
                query = pagination.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate)
                                                               : query.OrderBy(x => x.CreationDate);

                // Get total item count for pagination
                var totalItems = await query.CountAsync();

                // Paginate results
                var data = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                      .Take(pagination.PageSize)
                                      .ToListAsync();

                // Map the results to DTOs
                var groupDtos = data.ConvertAll(group => new OESGroupDto
                {
                    Id = group.Id.ToString(),
                    Name = group.Name,
                    Description = group.Description,
                    IsActive = group.IsActive,
                    Roles = group.Roles?.Select(role => new OESGroupRoleDto
                    {
                        RoleId = role.Id,
                        OrganizationSignature = role.OrganizationSignature
                    }).ToList() ?? []
                });

                // Return response with paginated data
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    "Groups fetched successfully with pagination!",
                    new CustomTableData<OESGroupDto>(groupDtos, totalItems)
                );
            }
            else
            {
                // If pagination is off, retrieve all data
                var data = await query.ToListAsync();

                // Map the results to DTOs
                var groupDtos = data.ConvertAll(group => new OESGroupDto
                {
                    Id = group.Id.ToString(),
                    Name = group.Name,
                    Description = group.Description,
                    IsActive = group.IsActive,
                    Roles = group.Roles?.Select(role => new OESGroupRoleDto
                    {
                        RoleId = role.Id,
                        OrganizationSignature = role.OrganizationSignature
                    }).ToList() ?? []
                });

                // Return response without pagination
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    "Groups fetched successfully without pagination!",
                    new CustomTableData<OESGroupDto>(groupDtos, data.Count)
                );
            }
        }

        public async Task<ApiResponse> GetGroupListAsync()
        {
            var query = _unitOfWork.Repository<OESGroup, Guid>().GetAll(g => !g.IsTemplate && !g.AutoCreatedForUser);

            var data = await query.ToListAsync();

            var mappedData = _mapper.Map<List<GetOESGroupDto>>(data);

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                System.Net.HttpStatusCode.OK,
                null,
                mappedData
            );
        }

        public async Task<ApiResponse> AddGroupAsync(NewGroupDto newGroupDto)
        {
            var Validator = await _appGroupValidatorService.ValidateForInsertionAsync(newGroupDto);

            if (Validator.CustomCodeStatus != CustomCodeStatus.Success)
                return Validator;

            var group = await CreateAndAddGroupAsync(newGroupDto);

            await AddGroupRoleAsync(await ListOfGroupRole(newGroupDto, group));

            if (await _unitOfWork.Complete() > 0)
            {
                var retrievedGroupDto = _mapper.Map<RetrivedGroupDto>(group);

                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    System.Net.HttpStatusCode.Created,
                                    @Resource.Groupwithitsroleshasbeenaddedsuccessfully,
                                    retrievedGroupDto);
            }
            else
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    System.Net.HttpStatusCode.BadRequest,
                                    Resource.FailedToAddGroup);
        }

        public async Task<ApiResponse> GetGroupDetailsAsync(Guid GroupId)
        {
            var Group = await _unitOfWork.Repository<OESGroup, Guid>().GetByIdAsync(GroupId);

            if (Group == null)
                return _apiResponse.GetApiResponse(CustomCodeStatus.NotFound, System.Net.HttpStatusCode.NotFound, "Group Not Found");

            var GroupRoles = _unitOfWork
                .Repository<OESGroupRole, long>()
                .GetAll(x => x.OESGroupId == GroupId, null, "OESRole")
                .Select(x => x.OESRole).ToList();

            OESGroupDetailsDto detailsDto = new OESGroupDetailsDto()
            {
                Id = GroupId,
                Name = Group.Name,
                Description = Group.Description,
            };

            foreach (var Role in GroupRoles)
            {
                detailsDto.RoleDtos.Add(new OESRoleDto()
                {
                    Id = Role.Id,
                    Name = Role.Name,

                });
            }

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, System.Net.HttpStatusCode.OK, null, detailsDto);
        }

        public async Task<ApiResponse> EditGroupAsync(GroupUpdateDto groupUpdateDto)
        {
            var validationResponse = await _appGroupValidatorService.ValidateForUpdateAsync(groupUpdateDto);

            if (validationResponse.CustomCodeStatus != CustomCodeStatus.Success)
                return validationResponse;

            var appGroup = await _unitOfWork
                .Repository<OESGroup, Guid>()
                .GetByIdAsync(groupUpdateDto.Id);

            if (appGroup == null)
                return _apiResponse.GetApiResponse(CustomCodeStatus.GroupNotFound, HttpStatusCode.NotFound, "Group not found.");

            var similarNameExists = await _unitOfWork
                .Repository<OESGroup, Guid>()
                .IsExistAsync(e => e.Name.ToLower() == groupUpdateDto.GroupName.ToLower() && e.Id != groupUpdateDto.Id);

            if (similarNameExists)
                return _apiResponse.GetApiResponse(CustomCodeStatus.AlreadyExist, HttpStatusCode.Conflict, Resource.GroupNameIsAlreadyExist);

            appGroup.Name = groupUpdateDto.GroupName;
            appGroup.Description = groupUpdateDto.Description;
            appGroup.IsActive = groupUpdateDto.IsActive;

            _unitOfWork.Repository<OESGroup, Guid>().Update(appGroup);

            var newRoles = await GetOESGroupRolesAsync(groupUpdateDto);

            var existingGroupRoles = _unitOfWork
                .Repository<OESGroupRole, long>()
                .GetAll(x => x.OESGroupId == appGroup.Id)
                .ToList();

            if (existingGroupRoles.Any())
            {
                _unitOfWork.Repository<OESGroupRole, long>().DeleteRange(existingGroupRoles);
            }

            _unitOfWork.Repository<OESGroupRole, long>().AddRangAsync(newRoles);

            var previousIsActiveFilterSetting = _FilterParamssValues.ApplyIsActiveFilter;

            _FilterParamssValues.ApplyIsActiveFilter = false;

            try
            {
                var groupUsers = _unitOfWork
                    .Repository<AppUserProfileGroup, long>()
                    .GetAll(result => result.OESGroupId == appGroup.Id)
                    .ToList();

                if (groupUsers.Any())
                {
                    foreach (var userGroup in groupUsers)
                    {
                        userGroup.IsActive = groupUpdateDto.IsActive;
                    }

                    _unitOfWork.Repository<AppUserProfileGroup, long>().UpdateRange(groupUsers);
                }
            }
            finally
            {
                _FilterParamssValues.ApplyIsActiveFilter = previousIsActiveFilterSetting;
            }

            var result = await _unitOfWork.Complete();

            if (result > 0)
            {
                return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.GroupUpdatedSuccessfully);
            }
            else
            {
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.NotModified, Resource.FailedToUpdateGroup);
            }
        }

        public async Task<ApiResponse> GetAssignedEntityIdsAsync(Guid groupId, ResourceType resourceType)
        {
            List<long> ids = resourceType switch
            {
                ResourceType.Schedule => await _unitOfWork
                    .Repository<ScheduleGroups, long>()
                    .GetAll(x => x.OESGroupId == groupId && !x.OESGroup.IsTemplate)
                    .Select(x => x.ScheduleId)
                    .ToListAsync(),

                ResourceType.Papers => await _unitOfWork
                    .Repository<PaperGroups, long>()
                    .GetAll(x => x.OESGroupId == groupId && !x.OESGroup.IsTemplate)
                    .Select(x => x.PaperId)
                    .ToListAsync(),

                ResourceType.Questions => await _unitOfWork
                    .Repository<QuestionGroups, long>()
                    .GetAll(x => x.OESGroupId == groupId && !x.OESGroup.IsTemplate)
                    .Select(x => x.QuestionId)
                    .ToListAsync(),

                ResourceType.ItemBank => await _unitOfWork
                    .Repository<ItemBankGroups, long>()
                    .GetAll(x => x.OESGroupId == groupId && !x.OESGroup.IsTemplate)
                    .Select(x => x.ItemBankId)
                    .ToListAsync(),

                ResourceType.Ilo => await _unitOfWork
                    .Repository<ILOGroup, long>()
                    .GetAll(x => x.GroupId == groupId && !x.IsDeleted && !x.OESGroup.IsTemplate)
                    .Select(x => x.ILOId)
                    .ToListAsync(),

                _ => []
            };

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, ids);
        }

        public async Task<ApiResponse> GetGroupAndRolesByID(Guid GroupId)
        {
            var appGroup = await _unitOfWork
                .Repository<OESGroup, Guid>()
                .GetObjAsync(e => e.Id == GroupId);

            var appGroupRoles = _unitOfWork
                .Repository<OESGroupRole, long>()
                .GetAll(e => e.OESGroupId == appGroup.Id)
                .Select(e => e.OESRoleId)
                .ToList();

            if (appGroup != null)
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    "Successful Fetching!",
                                    new GroupUpdateDto
                                    {
                                        Id = appGroup.Id,
                                        GroupName = appGroup.Name,
                                        Description = appGroup.Description,
                                        RolesId = appGroupRoles,
                                        IsActive = appGroup.IsActive
                                    });
            }

            return _apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                "Group Not Found!");
        }

        public async Task<ApiResponse> GetGroupUsers(Guid groupId)
        {
            var userGroupList = await _unitOfWork
                                    .Repository<AppUserProfileGroup, long>()
                                    .GetAllAsync(userGroup => userGroup.OESGroupId == groupId, Including: "AppUserProfile");

            var users = new List<AppUserProfile>();

            foreach (var userGroup in userGroupList)
            {
                users.Add(userGroup.AppUserProfile);
            }

            var usersDtos = new List<GetGroupUserDto>();

            foreach (var userGroup in userGroupList)
            {
                var userDto = new GetGroupUserDto
                {
                    Id = userGroup.AppUserProfile.Id,
                    DisplayedName = userGroup.AppUserProfile.Username
                };

                usersDtos.Add(userDto);
            }

            return _apiResponse
               .GetApiResponse(CustomCodeStatus.Success,
                            HttpStatusCode.OK,
                            null,
                            usersDtos);
        }

        public async Task<ApiResponse> DeleteGroupAsync(Guid Id)
        {
            var repository = _unitOfWork.Repository<OESGroup, Guid>();

            var group = await repository.GetByIdAsync(Id);

            if (group == null)
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    System.Net.HttpStatusCode.NotFound,
                                    "Group Not found");
            }

            repository.SoftDelete(group);

            var result = await _unitOfWork.Complete();

            if (result > 0)
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    System.Net.HttpStatusCode.OK,
                                    Resource.GroupHasBeenDeletedSuccessfully);
            }

            return _apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    System.Net.HttpStatusCode.BadRequest,
                                    Resource.FailedToDeleteGroup);
        }

        public async Task<ApiResponse> AssignGroupToResourceAsync(long entityId, Guid groupId, int resourceTypeInt, bool assign)
        {
            var resourceType = (ResourceType)resourceTypeInt;

            switch (resourceType)
            {
                case ResourceType.Questions:
                    await HandleAssignmentAsync<QuestionGroups>(e => e.QuestionId == entityId && e.OESGroupId == groupId,
                        () => new QuestionGroups
                        {
                            QuestionId = entityId,
                            OESGroupId = groupId
                        }, assign);
                    break;

                case ResourceType.Papers:
                    await HandleAssignmentAsync<PaperGroups>(e => e.PaperId == entityId && e.OESGroupId == groupId,
                        () => new PaperGroups
                        {
                            PaperId = entityId,
                            OESGroupId = groupId
                        }, assign);
                    break;
            }

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching);
        }

        private async Task HandleAssignmentAsync<T>(
           Expression<Func<T, bool>> match,
           Func<T> create,
           bool assign
        ) where T : class
        {
            var repo = _unitOfWork.Repository<T, long>();

            var existing = await repo.GetObjAsync(match);

            if (assign && existing == null)
            {
                await repo.AddAsync(create());
                await _unitOfWork.Complete();
            }
            else if (!assign && existing != null)
            {
                repo.Delete(existing);
                await _unitOfWork.Complete();
            }
        }

        #region Group Adding Helper Methods
        private async Task<OESGroup> CreateAndAddGroupAsync(NewGroupDto newGroupDto)
        {
            var group = new OESGroup
            {
                Name = newGroupDto.GroupName,
                Description = newGroupDto.Description,

            };
            await _unitOfWork.Repository<OESGroup, long>().AddAsync(group);
            return group;
        }

        private async Task AddGroupRoleAsync(List<OESGroupRole> groupRoles)
        {
            if (groupRoles.Count > 0)
                _unitOfWork.Repository<OESGroupRole, long>().AddRangAsync(groupRoles);
        }

        private async Task<List<OESGroupRole>> ListOfGroupRole(NewGroupDto newGroupDto, OESGroup group)
        {
            var groupRoles = new List<OESGroupRole>();

            foreach (var roleId in newGroupDto.RolesId)
            {
                groupRoles.Add(new OESGroupRole
                {
                    OESGroup = group,
                    OESRoleId = roleId,
                });
            }

            return groupRoles;
        }

        public async Task<List<OESGroupRole>> GetOESGroupRolesAsync(GroupUpdateDto groupUpdateDto)
        {
            List<OESGroupRole> roles = new List<OESGroupRole>();

            var appGroup = await _unitOfWork
                .Repository<OESGroup, Guid>()
                .GetByIdAsync(groupUpdateDto.Id);

            foreach (var roleId in groupUpdateDto.RolesId)
            {
                roles.Add(new OESGroupRole
                {
                    OESGroupId = appGroup.Id,
                    OESGroup = appGroup,
                    OESRoleId = roleId,
                    IsActive = true,
                    IsDeleted = false,
                    OrganizationSignature = appGroup.OrganizationSignature
                });
            }
            return roles;
        }
        #endregion Group Adding Helper Methods
    }
}
