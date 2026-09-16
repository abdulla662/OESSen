namespace OES.Interface.GenericMemoryCacheRepository
{
    public interface IMemoryCacheRepository
    {
        bool SetItemInCache<TItem, TKey>(TKey key, TItem item, TimeSpan? absoluteExpiration = null) where TKey : struct;

        TItem? GetItemFromCache<TItem, TKey>(TKey key, bool removeAfterGet = false) where TKey : struct;

        void RemoveItemFromCache<TKey>(TKey key) where TKey : struct;
    }
}
