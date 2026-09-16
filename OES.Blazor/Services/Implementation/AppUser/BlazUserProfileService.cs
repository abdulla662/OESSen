using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.AppUser
{
    public class BlazUserProfileService : IBlazUserProfileService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<AppUserProfileRetrievalDto> _blazGetCustomTableData;

        public BlazUserProfileService(IHttpClientHelper httpClientHelper,
                                      IBlazGetCustomTableData<AppUserProfileRetrievalDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<ApiResponse> AssignAndUnassignUserToGroupAsync(UsersToGroupDto usersToGroupDto)
        {
            return await _httpClientHelper.PostAsync(usersToGroupDto, $"api/UserProfile/AssignAndUnassignUserToGroup");
        }

        public async Task<List<BlazUserDTO>> GetAllAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<BlazUserDTO>>($"api/UserProfile/GetAllUsers");

            return (List<BlazUserDTO>)response.Data;
        }

        public async Task<List<Guid>> GetAssignedUserIdsAsync(Guid groupId)
        {
            var response = await _httpClientHelper.GetAsync<List<Guid>>($"api/UserProfile/GetAssignedUserIds?groupId={groupId}");

            if (response?.Data is List<Guid> list)
                return list;

            return [];
        }

        public async Task<List<UserAccessByResourceDto>> GetUserAccessByResourceTypeAsync(Guid userId, ResourceType resourceType)
        {
            var response = await _httpClientHelper.GetAsync<List<UserAccessByResourceDto>>($"api/UserProfile/GetUserAccessByResourceType?userId={userId}&resourceType={(int)resourceType}");

            if (response?.Data is List<UserAccessByResourceDto> list)
                return list;

            return [];
        }

        public async Task<CustomTableData<BlazUserDTO>> GetUsersForSelectionAsync(PaginationSearchModel pagination)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(pagination, "api/UserProfile/GetAllUserPaginationAsync");

            var mappedItems = response.Items?
                .Select(u => new BlazUserDTO { ID = u.Id, Name = u.UserName })
                .ToList() ?? [];

            return new CustomTableData<BlazUserDTO>(mappedItems, response.TotalItems);
        }
    }
}