using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IAppRoleService
    {
        Task<IApiResponse> GetRolesAsync();
        Task<IApiResponse> GetPageRoles(long pageId);
        Task<IApiResponse> GetAllPagesRoles();
    }
}
