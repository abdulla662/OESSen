using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Interface.Interfaces
{
    public interface IPathsService
    {
        IEnumerable<ApiEndPointDTO> GetControllerPaths();
        Task<ApiResponse> GetAll();
        Task<ApiResponse> AssignRoleToPath(AssignRoleToEndpointDto AssignPatheDTO);
        Task<ApiResponse> GetPathRole(long PathId);
    }
}
