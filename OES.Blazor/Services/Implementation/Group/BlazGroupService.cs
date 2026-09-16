using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Group;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.Group
{
    public class BlazGroupService : IBlazGroupService
    {
        private readonly IBlazGetCustomTableData<OESGroupDto> _blazGetCustomTableData;
        private readonly IHttpClientHelper _httpClient;

        public BlazGroupService(IHttpClientHelper httpClient,
                                IBlazGetCustomTableData<OESGroupDto> blazGetCustomTableData,
                                GlobalUserContext globalUserContext)
        {
            _httpClient = httpClient;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<List<long>> GetAssignedEntityIdsAsync(Guid groupId, ResourceType resourceType)
        {
            var response = await _httpClient.GetAsync<List<long>>($"api/AppGroup/GetAssignedEntityIds?groupId={groupId}&resourceType={(int)resourceType}");

            if (response?.Data is System.Text.Json.JsonElement json)
                return json.Deserialize<List<long>>() ?? [];

            if (response?.Data is List<long> ids)
                return ids;

            return [];
        }

        public async Task<ApiResponse> CreateGroupAsync(NewGroupDto newGroupDto)
        {
            NewGroupDto newGroup = new NewGroupDto
            {
                GroupName = newGroupDto.GroupName,
                Description = newGroupDto.Description,
                RolesId = newGroupDto.RolesId,
            };

            return await _httpClient.PostAsync(newGroup, "api/AppGroup/AddGroup");
        }

        public async Task<ApiResponse> DeleteGroupAsync(Guid id)
        {
            return await _httpClient.GetAsync<ApiResponse>($"api/AppGroup/DeleteGroup?id={id}");
        }

        public async Task<CustomTableData<OESGroupDto>> GetPaginatedGroupsAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/AppGroup/GetAllGroups");
        }

        public async Task<List<GetOESGroupDto>> GetGroupsAsync()
        {
            var apiResponse = await _httpClient.GetAsync<List<GetOESGroupDto>>($"api/AppGroup/GetAllGroupList");

            return (List<GetOESGroupDto>)apiResponse.Data;
        }

        public async Task<OESGroupDetailsDto> GetGroupDetails(Guid id)
        {
            var response = await _httpClient.GetAsync<OESGroupDetailsDto>("api/AppGroup/GroupDetails?id=" + id);

            return (OESGroupDetailsDto)(response?.Data);
        }

        public async Task<GroupUpdateDto> GetByID(Guid groupId)
        {
            var ApiResposne = await _httpClient.GetAsync<GroupUpdateDto>($"api/AppGroup/GetGroupAndRolesByID?id={groupId}");

            return (GroupUpdateDto)ApiResposne.Data;
        }

        public async Task<ApiResponse> UpdateGroupAsync(GroupUpdateDto groupUpdateDto)
        {
            return await _httpClient.PostAsync(groupUpdateDto, $"api/AppGroup/EditGroup");
        }

        public async Task<List<GetGroupUserDto>> GetGroupUsers(Guid groupId)
        {
            var apiResponse = await _httpClient.GetAsync<List<GetGroupUserDto>>($"api/AppGroup/GetGroupUsers/{groupId}");

            return (List<GetGroupUserDto>)apiResponse.Data;
        }

        public async Task<ApiResponse> AssignGroupToResourceAsync(long entityId, Guid groupId, ResourceType resourceType, bool assign)
            => await _httpClient.PostAsync(null, $"api/AppGroup/AssignGroupToResource?entityId={entityId}&groupId={groupId}&resourceType={(int)resourceType}&assign={assign}");
    }
}
