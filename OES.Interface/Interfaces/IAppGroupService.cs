using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IAppGroupService
    {
        Task<ApiResponse> AddGroupAsync(NewGroupDto newGroupDto);
        Task<ApiResponse> GetGroupDetailsAsync(Guid GroupId);
        Task<ApiResponse> EditGroupAsync(GroupUpdateDto groupUpdateDto);
        Task<IApiResponse> GetPaginatedGroupsAsync(PaginationSearchModel pagination);
        Task<ApiResponse> GetGroupListAsync();
        Task<ApiResponse> GetGroupAndRolesByID(Guid GroupId);
        Task<ApiResponse> GetGroupUsers(Guid GroupId);
        Task<ApiResponse> DeleteGroupAsync(Guid Id);
        Task<ApiResponse> AssignGroupToResourceAsync(long entityId, Guid groupId, int resourceType, bool assign);
        Task<ApiResponse> GetAssignedEntityIdsAsync(Guid groupId, ResourceType resourceType);
    }
}
