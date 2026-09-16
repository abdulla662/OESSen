using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class OrganizationStructureController : OESBaseController
    {
        private readonly IOrganizationStructureService _organizationStructureService;


        public OrganizationStructureController(IOrganizationStructureService organizationStructureService)
        {
            _organizationStructureService = organizationStructureService;
        }


        // Organization Structure Operations

        [HttpGet("GetFlattenedOrganizationStructure")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetFlattenedOrganizationStructureAsync(long rootId)
        {
            return await _organizationStructureService.GetFlattenedOrganizationStructureAsync(rootId);
        }

        [HttpGet("GetAllOrganizationStructureRoots")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllOrganizationStructureRootsAsync()
        {
            return await _organizationStructureService.GetAllOrganizationStructureRootsAsync();
        }

        [HttpPost("GetAllOrganizationStructure")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllOrganizationStructureAsync(PaginationSearchModel pagination)
        {
            return await _organizationStructureService.GetAllOrganizationStructureAsync(pagination);
        }

        [HttpGet("GetOrganizationStructureById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetOrganizationStructureByIdAsync(long OrganizationStructureId)
        {
            return await _organizationStructureService.GetOrganizationStructureByIdAsync(OrganizationStructureId);
        }

        [HttpPost("GetOrganizationRootIdByLookupIds")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetOrganizationRootIdByLookupIdsAsync(List<long> lookupIds)
        {
            return await _organizationStructureService.GetOrganizationRootIdByLookupIdsAsync(lookupIds);
        }


        // Organization Root Operations

        [HttpPost("AddOrganizationStructureRoot")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto addOrganizationStructureRootDto)
        {
            return await _organizationStructureService.AddOrganizationStructureRootAsync(addOrganizationStructureRootDto);
        }

        [HttpPut("UpdateOrganizationStructureRoot")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto addOrganizationStructureRootDto)
        {
            return await _organizationStructureService.UpdateOrganizationStructureRootAsync(addOrganizationStructureRootDto);
        }

        [HttpDelete("DeleteOrganizationStructureRoot")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteOrganizationStructureRootAsync(long rootId)
        {
            return await _organizationStructureService.DeleteOrganizationStructureRootAsync(rootId);
        }


        // Organization Node Operations

        [HttpPost("AddOrganizationStructureNode")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto addOrganizationStructureNodeDto)
        {
            return await _organizationStructureService.AddOrganizationStructureNodeAsync(addOrganizationStructureNodeDto);
        }

        [HttpPut("UpdateOrganizationStructureNode")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto updateOrganizationStructureNodeDto)
        {
            return await _organizationStructureService.UpdateOrganizationStructureNodeAsync(updateOrganizationStructureNodeDto);
        }

        [HttpDelete("DeleteOrganizationStructureNode")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteOrganizationStructureNodeAsync(long nodeId)
        {
            return await _organizationStructureService.DeleteOrganizationStructureNodeAsync(nodeId);
        }


        // Organization Node Lookup Items Operations

        [HttpGet("GetNodeLookupItems")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetNodeLookupItemsAsync(long nodeId)
        {
            return await _organizationStructureService.GetNodeLookupItemsAsync(nodeId);
        }

        [HttpGet("GetAllNodeLookupItems")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllNodeLookupItemsAsync()
        {
            return await _organizationStructureService.GetAllNodeLookupItemsAsync();
        }

        [HttpGet("GetChildLookupItemsByParentLookupItemId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetChildLookupItemsByParentLookupItemIdAsync(long parentLookupItemId)
        {
            return await _organizationStructureService.GetChildLookupItemsByParentLookupItemIdAsync(parentLookupItemId);
        }

        [HttpPost("AddNodeLookupItem")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto addOrganizationNodeLookupItemRequestDto)
        {
            return await _organizationStructureService.AddNodeLookupItemAsync(addOrganizationNodeLookupItemRequestDto);
        }

        [HttpPut("UpdateNodeLookupItem")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto updateOrganizationNodeLookupItemRequestDto)
        {
            return await _organizationStructureService.UpdateNodeLookupItemAsync(updateOrganizationNodeLookupItemRequestDto);
        }

        [HttpDelete("DeleteNodeLookupItem")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteNodeLookupItemAsync(long nodeLookupItemId)
        {
            return await _organizationStructureService.DeleteNodeLookupItemAsync(nodeLookupItemId);
        }
    }
}