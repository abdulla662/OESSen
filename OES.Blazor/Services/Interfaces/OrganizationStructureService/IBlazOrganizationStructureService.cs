using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.OrganizationStructureService
{
    public interface IBlazOrganizationStructureService
    {
        // Organization Structure Operations

        Task<List<TreeItemResponseDto>> GetOrganizationStructureTreeByRootId(long IdCarrier);

        Task<GetOrganizationRootResponseDto[]> GetAllOrganizationStructureRootsAsync();

        Task<GetOrganizationNodeResponseDto[]> GetFlattenedOrganizationStructureAsync(long rootId);

        Task<CustomTableData<GetAllOrganizationResponseDto>> GetAllOrganizationStructureAsync(PaginationSearchModel pagination);

        Task<AddOrUpdateOrganizationStructureRootRequestDto> GetOrganizationStructureByIdAsync(long OrganizationStructureId);

        Task<ApiResponse> GetOrganizationRootIdByLookupIdsAsync(List<long> lookupIds);


        // Organization Root Operations

        Task<ApiResponse> AddOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto addOrganizationStructureRootDto);

        Task<ApiResponse> UpdateOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto updateOrganizationStructureRootDto);

        Task<ApiResponse> DeleteOrganizationStructureRootAsync(long rootId);


        // Organization Node Operations

        Task<ApiResponse> AddOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto addOrganizationStructureNodeDto);

        Task<ApiResponse> UpdateOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto updateOrganizationStructureNodeDto);

        Task<ApiResponse> DeleteOrganizationStructureNodeAsync(long nodeId);


        // Organization Node Lookup Items Operations

        Task<List<GetOrganizationNodeLookupItemResponseDto>> GetNodeLookupItemsAsync(long nodeId);

        Task<List<GetOrganizationNodeLookupItemForViewResponseDto>> GetChildLookupItemsByParentLookupItemIdAsync(long parentLookupItemId);

        Task<ApiResponse> AddNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto addOrganizationNodeLookupItemRequestDto);

        Task<ApiResponse> UpdateNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto updateOrganizationNodeLookupItemRequestDto);

        Task<ApiResponse> DeleteNodeLookupItemAsync(long nodeLookupItemId);
    }
}
