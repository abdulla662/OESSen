using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Blazor.Services.Interfaces.MemoryCache
{
    public interface IBlazMemoryCache
    {
        Task<PageRoleDTO> GetPageRoles(string pageName);
    }
}
