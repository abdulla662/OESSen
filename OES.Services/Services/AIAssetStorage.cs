using OES.Helper.General;
using OES.Interface.GenericMemoryCacheRepository;

namespace OES.Services.Services
{
    public class AIAssetStorage
    {
        private readonly IMemoryCacheRepository _cacheRepository;

        public AIAssetStorage(IMemoryCacheRepository cacheRepository)
        {
            _cacheRepository = cacheRepository;
        }

        public Guid Save(byte[] imageBytes, string contentType)
        {
            var assetId = Guid.NewGuid();

            var asset = new AIAsset
            {
                Id = assetId,
                Data = imageBytes,
                ContentType = contentType
            };

            _cacheRepository.SetItemInCache(assetId, asset, TimeSpan.FromMinutes(30));

            return assetId;
        }

        public AIAsset? Get(Guid assetId)
        {
            return _cacheRepository.GetItemFromCache<AIAsset, Guid>(assetId);
        }

        public void Remove(Guid assetId)
        {
            _cacheRepository.RemoveItemFromCache(assetId);
        }
    }
}
