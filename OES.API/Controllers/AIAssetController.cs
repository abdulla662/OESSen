using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Services.Services;

namespace OES.API.Controllers
{
    public class AIAssetController : OESBaseController
    {
        private readonly AIAssetStorage _aiAssetStorage;

        public AIAssetController(AIAssetStorage aiAssetStorage)
        {
            _aiAssetStorage = aiAssetStorage;
        }

        [OESFilter(Authorize = false)]
        [DoNotEncrypt]
        [HttpGet("GetAIAsset")]
        public IActionResult GetAIAsset(Guid assetId)
        {
            var asset = _aiAssetStorage.Get(assetId);

            if (asset == null)
            {
                return NotFound();
            }

            return File(asset.Data, asset.ContentType);
        }
    }
}
