using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.ItemBankLevels
{
    public interface IBlazorItemBankLevelsService
    {
        Task<List<ItemLevelsDto>> GetLevels();
        Task<ApiResponse> AddLevel(ItemLevelsDto itemLevelsDto);

    }
}
