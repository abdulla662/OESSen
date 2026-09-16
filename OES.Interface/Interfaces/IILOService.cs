using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IILOService
    {
        Task<IApiResponse> GetByIdAsync(long id);
        Task<ApiResponse> AddNewRoot(ILONewRootDto dto);
        Task<ApiResponse> EditILORoot(IloEditDTO dto);
        Task<ApiResponse> GetILORoot(long Id);
        Task<List<TreeItemResponseDto>> GetByParentIdAndSignatureAsync(TreeItemRequestDto iloRequestDto);
        Task<ApiResponse> InsertNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto);
        Task<IApiResponse> EditNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto);
        Task<ApiResponse> GetParentsAsync(long? parentId);
        Task<IApiResponse> SoftDeleteRootWithChildrenAsync(long id);
        Task<IApiResponse> SoftDeleteRootAsync(long id);
        Task<IApiResponse> GetPagedILOS(PaginationSearchModel pagination);
        Task<IApiResponse> CanSoftDeleteIloAsync(long id);
        Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long id);
        Task<ApiResponse> TransferChildrenForNodeAsync(long id);
        Task<IApiResponse> GetRootNodeILO();
        Task<ApiResponse> GetIloGroupsAsync(long iloId);
        Task<ApiResponse> GetUserIloGroupAsync();
    }
}
