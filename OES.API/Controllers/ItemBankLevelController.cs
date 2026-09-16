using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
namespace OES.API.Controllers
{
    public class ItemBankLevelController : OESBaseController
    {
        private readonly IItemBankLevelService _itemBankLevelsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemBankLevelController"/> class.
        /// </summary>
        /// <param name="itemBankLevelsService">The item bank level service.</param>
        public ItemBankLevelController(IItemBankLevelService itemBankLevelsService)
        {
            _itemBankLevelsService = itemBankLevelsService;
        }

        /// <summary>
        /// Retrieves all Item Bank Levels.
        /// </summary>
        /// <returns>An IApiResponse containing a list of all Item Bank Levels.</returns>
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        [HttpGet("GetLevels")]
        public IApiResponse GetLevels()
        {
            return _itemBankLevelsService.GetAllLevels();
        }


        /// <summary>
        /// Adds a new Item Bank Level.
        /// </summary>
        /// <param name="newLevel">The details of the new Item Bank Level to be added.</param>
        /// <returns>An IApiResponse indicating the result of the add operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddLevel")]
        public async Task<IApiResponse> AddLevel(ItemLevelsDto newLevel)
        {
            return await _itemBankLevelsService.AddLevel(newLevel);
        }
    }
}
