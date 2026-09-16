using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.AppUser
{
    public interface IBlazUserProfileService
    {
        Task<ApiResponse> AssignAndUnassignUserToGroupAsync(UsersToGroupDto usersToGroupDto);

        Task<List<BlazUserDTO>> GetAllAsync();

        Task<List<Guid>> GetAssignedUserIdsAsync(Guid groupId);

        Task<CustomTableData<BlazUserDTO>> GetUsersForSelectionAsync(PaginationSearchModel pagination);

        Task<List<UserAccessByResourceDto>> GetUserAccessByResourceTypeAsync(Guid userId, ResourceType resourceType);
    }
}
