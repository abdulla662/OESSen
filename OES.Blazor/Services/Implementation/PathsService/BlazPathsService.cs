using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.PathsService;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Implementation.PathsService
{
    public class BlazPathsService(IHttpClientHelper _clientHelper) : IBlazPathsService
    {
        public async Task<ApiResponse> AssignRolesToPath(AssignRoleToEndpointDto assignRoleToPathDTO)
        {
            return await _clientHelper.PostAsync(assignRoleToPathDTO, "api/Paths/AssignRolesToPath");
        }

        public async Task<ApiResponse> GetAll()
        {
            return await _clientHelper.GetAsync<List<BlazPathDTO>>($"api/Paths/GetAllPathes");
        }

        public async Task<List<RoleDto>> GetPathRoles(long id)
        {
            var apiResposnse = await _clientHelper.GetAsync<List<RoleDto>>($"api/Paths/GetPathRoles?PathId={id}");
            return (List<RoleDto>)apiResposnse.Data;
        }
    }
}
