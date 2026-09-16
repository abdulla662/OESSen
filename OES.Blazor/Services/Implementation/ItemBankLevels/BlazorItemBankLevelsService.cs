using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.ItemBankLevels;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.ItemBankLevels
{
    public class BlazorItemBankLevelsService : IBlazorItemBankLevelsService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazorItemBankLevelsService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> AddLevel(ItemLevelsDto newLevel)
        {
            var Response = await _httpClientHelper.PostAsync(newLevel, "api/ItemBankLevel/AddLevel");

            return Response;
        }

        public async Task<List<ItemLevelsDto>> GetLevels()
        {
            var result = await _httpClientHelper.GetAsync<List<ItemLevelsDto>>($"api/ItemBankLevel/GetLevels");

            return (List<ItemLevelsDto>)result.Data;
        }
    }
}
