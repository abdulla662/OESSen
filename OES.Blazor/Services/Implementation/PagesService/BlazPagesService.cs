using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.PagesService;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Implementation.PagesService
{
    public class BlazPagesService : IBlazPagesService
    {
        private readonly IHttpClientHelper _httpClient;

        public BlazPagesService(IHttpClientHelper httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse> GetAll()
        {
            return await _httpClient.GetAsync<List<BalzPageDTO>>($"api/Page/Getall");

        }

        public async Task<ApiResponse> AssignRolesToPage(AssignRoleToPageDTO assignRoleToPageDTO)
        {
            return await _httpClient.PostAsync(assignRoleToPageDTO, "api/Page/AssignRolesToPage");
        }

        public async Task<List<RoleDto>> GetPageRoles(long id)
        {
            var apiResposnse = await _httpClient.GetAsync<List<RoleDto>>($"api/Page/GetPageRoles?PageId={id}");
            return (List<RoleDto>)apiResposnse.Data;
        }
    }
}
