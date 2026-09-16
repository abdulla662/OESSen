using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Interface.Interfaces
{
    public interface IPageService
    {
        Task<ApiResponse> GetAll();
        Task<ApiResponse> AssignRoleToPage(AssignRoleToPageDTO AssignpageDTO);
        Task<ApiResponse> GetPageRole(long pageId);
    }
}
