using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IUserProfileService
    {
        Task<IApiResponse> ValidateTokenForUserAsync(string? token);

        Task<ApiResponse> GetAllAsync();

        Task<ApiResponse> GetUserGroupsAndRolesAsync(Guid userId);

        Task<ApiResponse> GetAllUserPaginationAsync(PaginationSearchModel pagination);

        Task<ApiResponse> AssignAndUnassignUserToGroupAsync(UsersToGroupDto usersToGroupDto);

        Task<ApiResponse> LogInAsync(LoginDto loginDto);

        Task<ApiResponse> GetAssignedUserIdsByGroupAsync(Guid groupId);

        Task<ApiResponse> GetUserAccessByResourceTypeAsync(Guid userId, ResourceType resourceType);
    }
}
