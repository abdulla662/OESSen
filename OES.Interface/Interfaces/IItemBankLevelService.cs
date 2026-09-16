using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IItemBankLevelService
    {
        ApiResponse GetAllLevels();

        Task<ApiResponse> AddLevel(ItemLevelsDto itemBankLevel);

    }
}
