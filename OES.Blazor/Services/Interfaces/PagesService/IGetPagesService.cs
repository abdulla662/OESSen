using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.PagesService
{
    public interface IGetPagesService
    {
        List<PageDTO> GetPages();

        Task<ApiResponse> sendPagesListToAPI();
    }
}
