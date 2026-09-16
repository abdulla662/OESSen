using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Blazor.Services.Interfaces.ILOSection
{
    public interface IBlazILOService
    {
        Task<ApiResponse> AddRoot(ILONewRootDto iLONewRootDto);
        Task<ApiResponse> InsertNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto);
        Task<ApiResponse> EditNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto);
        Task<ApiResponse?> EditILORoot(IloEditDTO IloEditDTO);
        Task<ApiResponse> GetILORoot(long Id);
        Task<List<ParentItemDto>> GetParentsAsync(long? parentId);
        Task<ApiResponse> CanSoftDeleteIloRootAsync(long id);
        Task<ApiResponse> SoftDeleteILORoot(long id);
        Task<IApiResponse> SoftDeleteWithChildernILORoot(long id);
        Task<CustomTableData<ILODto>> GetAllRoots(PaginationSearchModel pagination);
        Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long itemId);
        Task<ApiResponse> TransferChildrenForNodeAsync(long itemId);
        Task<List<TreeItemResponseDto>> GetChildrenByParentIdAsync(long parentId, string signature);
        Task<List<RootIloDto>> GetRootIlosNodesAsync();
        Task<IloGroupsDto> GetIloGroupsAsync(long iloId);
        Task<List<GetOESGroupDto>> GetUserIloGroupsAsync();
    }
}
