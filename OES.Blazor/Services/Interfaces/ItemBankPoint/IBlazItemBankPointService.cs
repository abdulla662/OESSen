using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.ItemBankPoint
{
    public interface IBlazItemBankPointService
    {
        Task<List<List<TreeItemResponseDto>>> GetPaperSelectedItemBanksAsync(long paperId);

        Task<ApiResponse> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto);

        Task<ApiResponse> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto updateItemBankPointRequestDto);
    }
}
