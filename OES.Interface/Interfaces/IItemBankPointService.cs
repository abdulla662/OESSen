using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IItemBankPointService
    {
        Task<IApiResponse> GetPaperSelectedItemBanksAsync(long paperId);

        Task<ApiResponse> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto);

        Task<IApiResponse> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto editItemBankPointRequestDto);
    }
}
