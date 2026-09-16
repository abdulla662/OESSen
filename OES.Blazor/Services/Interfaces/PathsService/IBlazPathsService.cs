using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.PathsService
{
    public interface IBlazPathsService
    {
        Task<ApiResponse> GetAll();
        Task<ApiResponse> AssignRolesToPath(AssignRoleToEndpointDto assignRoleToPathDTO);
        Task<List<RoleDto>> GetPathRoles(long id);
    }
}
