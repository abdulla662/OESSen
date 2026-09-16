using Microsoft.Extensions.Caching.Memory;
using OES.Blazor.Services.Interfaces.MemoryCache;
using OES.Blazor.Services.Interfaces.RolesService;
using OES.Helper.PagesEndpointsRolesDtos;
using SharedHelper.General;

namespace OES.Blazor.Services.Implementation.MemoryCache
{
    public class BlazMemoryCache : IBlazMemoryCache
    {
        private readonly IBlazRolesService _blazAppRoleService;

        private readonly IMemoryCache _memoryCache;

        public BlazMemoryCache(IBlazRolesService blazAppRoleService, IMemoryCache memoryCache)
        {
            _blazAppRoleService = blazAppRoleService;
            _memoryCache = memoryCache;
        }

        public async Task<PageRoleDTO> GetPageRoles(string pageName)
        {
            var cache = _memoryCache.Get("pages") as List<PageRoleDTO>;

            if (cache == null || cache.Count == 0)
            {
                var response = await _blazAppRoleService.GetAllPagesRoles();

                var all = response.Data as List<PageRoleDTO> ?? [];

                var options = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(5))
                    .SetAbsoluteExpiration(DateTimeHelper.Now.AddMinutes(60));

                _memoryCache.Set("pages", all, options);

                return all.Find(x => x.PageName == pageName);
            }

            return cache.Find(x => x.PageName == pageName);
        }
    }
}
