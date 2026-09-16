using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.UserService;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.User;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Implementation.UserService
{
    public class BlazUserService : IBlazUserService
    {
        private readonly IHttpClientHelper _clientHelper;
        private readonly IBlazGetCustomTableData<AppUserProfileRetrievalDto> _blazGetCustomTableData;

        public BlazUserService(IHttpClientHelper clientHelper, IBlazGetCustomTableData<AppUserProfileRetrievalDto> blazGetCustomTableData)
        {
            _clientHelper = clientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<ApiResponse> GetAll()
        {
            return await _clientHelper.GetAsync<List<BlazUserDTO>>("api/UserProfile/GetAllUsers");
        }

        public async Task<ApiResponse> AssignUsersToGroupAsync(UsersToGroupDto dto)
        {
            return await _clientHelper.PostAsync(dto, "api/UserProfile/AssignAndUnassignUserToGroup");
        }

        public async Task<CustomTableData<AppUserProfileRetrievalDto>> GetAllUserPaginationAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/UserProfile/GetAllUserPaginationAsync");
        }

        public async Task<List<RoleDto>> GetUserRoles(Guid id)
        {
            var apiResposnse = await _clientHelper.GetAsync<ApiResponse>($"api/UserProfile/GetUserRoles?UserId={id}");

            if (apiResposnse?.Data == null)
                return [];

            return apiResposnse.Data as List<RoleDto> ?? [];
        }

        public async Task<ApiResponse> SyncUsersProfiles(string organizationSignature)
        {
            var result = await _clientHelper.PostAsync(organizationSignature, $"api/UserProfile/SyncUsersProfiles?organizationSignature={organizationSignature}");

            return result;
        }

        public async Task<ApiResponse> AssignRolesToUser(AssignRoleToUserDto dto)
        {
            return await _clientHelper.PostAsync(dto, "api/UserProfile/AssignRolesToUser");
        }
    }
}
