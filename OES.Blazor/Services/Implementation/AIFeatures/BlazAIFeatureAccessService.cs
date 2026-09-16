using Microsoft.Extensions.Caching.Memory;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.General.GlobalUserContext;

namespace OES.Blazor.Services.Implementation.AIFeatures
{
    public class BlazAIFeatureAccessService : IBlazAIFeatureAccessService
    {
        private readonly GlobalUserContext _globalUserContext;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_PREFIX = "AIFeatures_Access_Org_";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        public BlazAIFeatureAccessService(
            GlobalUserContext globalUserContext,
            IMemoryCache cache
        )
        {
            _globalUserContext = globalUserContext;
            _cache = cache;
        }

        public async Task<bool> HasAIFeaturesAccessAsync(long organizationId)
        {
            return true;
        }
    }
}
