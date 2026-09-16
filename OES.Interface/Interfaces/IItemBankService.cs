using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IItemBankService
    {
        Task<IApiResponse> GetByIdAsync(long id);
        Task<List<TreeItemResponseDto>> GetByParentIdAndSignatureAsync(TreeItemRequestDto itemBankRequestDto);
        Task<IApiResponse> CanSoftDeleteItemBankAsync(long id);
        Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long id);
        Task<IApiResponse> ExecuteSoftDeleteForRootAsync(long id);
        Task<IApiResponse> UpdateItemBankAsync(NewItemBankDTO bankDTO);
        Task<IApiResponse> GetItemBankByIDAsync(long id);
        Task<IApiResponse> TransferChildrenForNodeAsync(long id);
        Task<ApiResponse> GetParentsForEditNodeObject(long Id);
        Task<ApiResponse> EditItemBankNode(EditItemBankNodeDto itemBankNodeDto);
        Task<ApiResponse> GetItemBankNodeObject(long Id);
        Task<ApiResponse> GetAllItemBankAsync(PaginationSearchModel pagination);
        Task<IApiResponse> AddItemBank(NewItemBankDTO itemBankDto);
        Task<IApiResponse> AddNewNode(ItemBankNode newItemBankNode);
        Task<ApiResponse> AddItemBankTreeAsync(List<AIItemBankNodeDto> trees);
        Task<IApiResponse> GetNodeValidation(long ParentId);
        Task<IApiResponse> GetRootNodeItemBank();
        Task<ApiResponse> GetItemBankGroupsAsync(long itemBankId);
        Task<ApiResponse> GetAllItemBanksListAsync();
        Task<ApiResponse> GetItemBankStatisticsAsync(long itemBankId);
        Task<ApiResponse> GetAllItemBankForUserAsync(long itemBankId);
        Task<ApiResponse> GetAllItemBankRootsForPaperAsync(PaginationSearchModel pagination);
        Task<ApiResponse> TransferQuestionsToItemBankAsync(TransferQuestionsToItemBankDto dto);
        Task<ApiResponse> GetUserItemBankGroupsAsync();
        Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole);
        Task<ApiResponse> AddTemplateAIItemBankAsync(AIItemBankTemplateCreationDto dto);
        Task<ApiResponse> GetAllItemBankTemplatesAsync(PaginationSearchModel paginationSearch, bool isFromItmBankAI);
        Task<ApiResponse> GetItemBankTemplateByIdAsync(long templateId);
        Task<ApiResponse> DeleteItemBankTemplateAsync(long templateId);
    }
}
