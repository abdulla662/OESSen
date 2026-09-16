using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.ItemBankPoint;
using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.ItemBankPoint
{
    public class BlazItemBankPointService : IBlazItemBankPointService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazItemBankPointService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<List<List<TreeItemResponseDto>>> GetPaperSelectedItemBanksAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<List<TreeItemResponseDto>>>($"api/ItemBankPoint/GetPaperSelectedItemBanks?{nameof(paperId)}={paperId}");

            return (List<List<TreeItemResponseDto>>)response.Data ?? [];
        }

        public async Task<ApiResponse> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(addItemBankPointRequestDto, "api/ItemBankPoint/AddItemBankPoint");

            return response;
        }

        public async Task<ApiResponse> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto updateItemBankPointRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(updateItemBankPointRequestDto, "api/ItemBankPoint/UpdateItemBankPoint");

            return response;
        }
    }
}
