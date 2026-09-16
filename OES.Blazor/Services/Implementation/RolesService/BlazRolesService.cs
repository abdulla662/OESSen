using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.RolesService;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using System.Net;

namespace OES.Blazor.Services.Implementation.RolesService
{
    public class BlazRolesService : IBlazRolesService
    {
        private readonly IHttpClientHelper _httpClient;

        public BlazRolesService(IHttpClientHelper httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var response = await _httpClient.GetAsync<List<RoleDto>>("api/Roles/getAllRoles");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Data as List<RoleDto>;
            }

            return [];
        }

        public async Task<ApiResponse> GetAllPagesRoles()
        {
            var response = await _httpClient.GetAsync<List<PageRoleDTO>>($"api/Roles/GetAllPagesRoles?NoCache={Guid.NewGuid()}");

            return response;
        }
    }
}
