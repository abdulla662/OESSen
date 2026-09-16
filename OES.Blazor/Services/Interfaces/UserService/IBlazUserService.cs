using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.UserService
{
    public interface IBlazUserService
    {
        Task<ApiResponse> GetAll();

        Task<ApiResponse> AssignRolesToUser(AssignRoleToUserDto assignRoleToUserDTO);

        Task<List<RoleDto>> GetUserRoles(Guid id);

        Task<ApiResponse> SyncUsersProfiles(string organizationSignature);

        Task<CustomTableData<AppUserProfileRetrievalDto>> GetAllUserPaginationAsync(PaginationSearchModel pagination);

        Task<ApiResponse> AssignUsersToGroupAsync(UsersToGroupDto dto);
    }
}
