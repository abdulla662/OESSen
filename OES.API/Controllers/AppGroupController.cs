using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class AppGroupController : OESBaseController
    {
        private readonly IAppGroupService _appGroupService;

        public AppGroupController(IAppGroupService appGroupService)
        {
            _appGroupService = appGroupService;
        }

        [HttpPost("GetAllGroups")]
        [OESFilter(Authorize = true, ApplySignatureFilter = true)]
        public async Task<IApiResponse> GetAllGroupsAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _appGroupService.GetPaginatedGroupsAsync(paginationSearchModel);
        }

        [HttpGet("GetAllGroupList")]
        [OESFilter(Authorize = true, ApplySignatureFilter = true)]
        public async Task<ApiResponse> GetAllGroupListsAsync()
        {
            return await _appGroupService.GetGroupListAsync();
        }

        [HttpPost("AddGroup")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddGroupAsync(NewGroupDto newGroupDto)
        {
            return await _appGroupService.AddGroupAsync(newGroupDto);
        }

        [HttpGet("GroupDetails")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetGroupDetails(Guid id)
        {
            return await _appGroupService.GetGroupDetailsAsync(id);
        }

        [HttpPost("EditGroup")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> EditGroupAsync(GroupUpdateDto groupUpdateDto)
        {
            return await _appGroupService.EditGroupAsync(groupUpdateDto);
        }

        [HttpGet("GetGroupAndRolesByID")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetByID(Guid id)
        {
            return await _appGroupService.GetGroupAndRolesByID(id);
        }

        [HttpGet("GetGroupUsers/{groupId}")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetGroupUsers(Guid groupId)
        {
            return await _appGroupService.GetGroupUsers(groupId);
        }

        [HttpGet("DeleteGroup")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteGroupAsync(Guid id)
        {
            return await _appGroupService.DeleteGroupAsync(id);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AssignGroupToResource")]
        public async Task<IApiResponse> AssignGroupToResource(long entityId, Guid groupId, int resourceType, bool assign)
        {
            return await _appGroupService.AssignGroupToResourceAsync(entityId, groupId, resourceType, assign);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAssignedEntityIds")]
        public async Task<IApiResponse> GetAssignedEntityIdsAsync(Guid groupId, int resourceType)
        {
            return await _appGroupService.GetAssignedEntityIdsAsync(groupId, (ResourceType)resourceType);
        }
    }
}
