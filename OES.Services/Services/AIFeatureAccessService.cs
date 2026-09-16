using Microsoft.Extensions.Caching.Memory;
using OES.Interface.Interfaces;

namespace OES.Services.Services
{
    public class AIFeatureAccessService : IAIFeatureAccessService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        public AIFeatureAccessService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<bool> HasAIFeaturesAccessAsync(long organizationId)
        {
            return true;
        }
    }
}
