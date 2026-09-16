using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.PagesService
{
    public interface IBlazPagesService
    {
        Task<ApiResponse> GetAll();
        Task<ApiResponse> AssignRolesToPage(AssignRoleToPageDTO assignRoleToPageDTO);
        Task<List<RoleDto>> GetPageRoles(long id);
    }
}
