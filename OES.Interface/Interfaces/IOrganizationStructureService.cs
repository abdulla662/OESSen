using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IOrganizationStructureService
    {
        // Organization Structure Operations

        Task<ApiResponse> GetFlattenedOrganizationStructureAsync(long rootId);

        Task<ApiResponse> GetAllOrganizationStructureRootsAsync();

        Task<ApiResponse> GetAllOrganizationStructureAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetOrganizationStructureByIdAsync(long OrganizationStructureId);

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

        Task<ApiResponse> GetNodeLookupItemsAsync(long nodeId);

        Task<ApiResponse> GetAllNodeLookupItemsAsync();

        Task<ApiResponse> GetChildLookupItemsByParentLookupItemIdAsync(long parentLookupItemId);

        Task<ApiResponse> AddNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto addOrganizationNodeLookupItemRequestDto);

        Task<ApiResponse> UpdateNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto updateOrganizationNodeLookupItemRequestDto);

        Task<ApiResponse> DeleteNodeLookupItemAsync(long nodeLookupItemId);
    }
}