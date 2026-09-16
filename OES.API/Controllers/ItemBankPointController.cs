using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ItemBankPointController(IItemBankPointService _itemBankPointService) : OESBaseController
    {
        [HttpGet("GetPaperSelectedItemBanks")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperSelectedItemBanksAsync(long paperId)
        {
            return await _itemBankPointService.GetPaperSelectedItemBanksAsync(paperId);
        }

        [HttpPost("AddItemBankPoint")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto)
        {
            return await _itemBankPointService.AddItemBankPointAsync(addItemBankPointRequestDto);
        }

        [HttpPost("UpdateItemBankPoint")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto editItemBankPointRequestDto)
        {
            return await _itemBankPointService.UpdateItemBankPointAsync(editItemBankPointRequestDto);
        }
    }
}