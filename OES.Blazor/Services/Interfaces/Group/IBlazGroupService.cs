using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Group
{
    public interface IBlazGroupService
    {
        Task<CustomTableData<OESGroupDto>> GetPaginatedGroupsAsync(PaginationSearchModel pagination);
        Task<List<GetOESGroupDto>> GetGroupsAsync();
        Task<ApiResponse> CreateGroupAsync(NewGroupDto newGroupDto);
        Task<ApiResponse> DeleteGroupAsync(Guid id);
        Task<ApiResponse> UpdateGroupAsync(GroupUpdateDto groupDTO);
        Task<OESGroupDetailsDto> GetGroupDetails(Guid id);
        Task<GroupUpdateDto> GetByID(Guid groupId);
        Task<List<GetGroupUserDto>> GetGroupUsers(Guid groupId);
        Task<ApiResponse> AssignGroupToResourceAsync(long entityId, Guid groupId, ResourceType resourceType, bool assign);
        Task<List<long>> GetAssignedEntityIdsAsync(Guid groupId, ResourceType resourceType);
    }
}
