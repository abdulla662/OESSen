using Microsoft.Extensions.Caching.Memory;
using OES.Interface.GenericMemoryCacheRepository;

namespace OES.Services.GenericMemoryCacheRepository
{
    public class MemoryCacheRepository : IMemoryCacheRepository
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheRepository(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public bool SetItemInCache<TItem, TKey>(TKey key, TItem item, TimeSpan? absoluteExpiration = null) where TKey : struct
        {
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromDays(1)
                };

                if (absoluteExpiration == null)
                {
                    cacheEntryOptions.SlidingExpiration = TimeSpan.FromDays(1);
                }

                _memoryCache.Set(key, item, cacheEntryOptions);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public TItem? GetItemFromCache<TItem, TKey>(TKey key, bool removeAfterGet = false) where TKey : struct
        {
            if (_memoryCache.TryGetValue(key, out TItem? item))
            {
                if (removeAfterGet)
                {
                    _memoryCache.Remove(key);
                }

                return item;
            }

            return default;
        }

        public void RemoveItemFromCache<TKey>(TKey key) where TKey : struct
        {
            _memoryCache.Remove(key);
        }
    }
}
