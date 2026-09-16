using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.RolesService;

public interface IBlazRolesService
{
    Task<List<RoleDto>> GetAllRolesAsync();

    Task<ApiResponse> GetAllPagesRoles();
}
