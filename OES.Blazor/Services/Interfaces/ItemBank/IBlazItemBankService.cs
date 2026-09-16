using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.ItemBank
{
    public interface IBlazItemBankService
    {
        Task<ApiResponse> UpdateItemBank(NewItemBankDTO itemBankDTO);
        Task<NewItemBankDTO> GetItemBankByID(long Id);
        Task<EditItemBankNodeDto> GetEditItemBank(long Id);
        Task<List<ItemParentList>> GetItemParentLists(long? Id);
        Task<ApiResponse> UpdateNode(EditItemBankNodeDto editItemBankNodeDto);
        Task<CustomTableData<NewItemBankDTO>> GetAllItemBankAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> AddItemBankAsync(NewItemBankDTO itemBankDto);
        Task<ApiResponse> CanSoftDeleteItemBankAsync(long itemId);
        Task ExecuteSoftDeleteForNodeAsync(long itemId);
        Task<ApiResponse> ExecuteSoftDeleteForRootAsync(long itemId);
        Task TransferChildrenForNodeAsync(long itemId);
        Task<ItemBankLevelValidation> GetNodeValidation(long ParentId);
        Task<ApiResponse> AddNewNode(ItemBankNode itemBankNode);
        Task<List<RootItemBankDto>> GetRootItemBanksNodesAsync();
        Task<ItemBankGroupsDto> GetItemBankGroupsAsync(long itemBankId);
        Task<List<RootItemBankDto>> GetItemBanksListAsync();
        Task<ItemBankStatisticsDto> GetItemBankStatistics(long itemBankId);
        Task<List<UserItemBankDto>> GetAllItemBankForUserAsync(long itemBankId);
        Task<CustomTableData<NewItemBankDTO>> GetAllItemBankRootsForPaperAsync(PaginationSearchModel pagination);
        Task<ApiResponse> TransferQuestionsToItemBankAsync(TransferQuestionsToItemBankDto dto);
        Task<List<GetOESGroupDto>> GetUserItemBankGroupsAsync();
        Task<List<TreeItemResponseDto>> GetChildrenByParentIdAsync(long parentId, string signature);
        Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole);

        // Template Methods:
        Task<ApiResponse> AddTemplateAIItemBankAsync(AIItemBankTemplateCreationDto dto);
        Task<CustomTableData<ItemBankTemplateDto>> GetAllItemBankTemplateAsync(ItemBankPaginationSearchRequest paginationSearch);
        Task<ApiResponse> GetItemBankTemplateById(long templateId);
        Task<ApiResponse> DeleteItemBankTemplateAsync(long templateId);
    }
}
