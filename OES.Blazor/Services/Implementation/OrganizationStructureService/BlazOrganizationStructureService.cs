using OES.Blazor.Protos;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.OrganizationStructureService
{
    public class BlazOrganizationStructureService : IBlazOrganizationStructureService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly OrganizationStructureTreeService.OrganizationStructureTreeServiceClient _organizationStructureTreeServiceClient;
        private readonly IBlazGetCustomTableData<GetAllOrganizationResponseDto> _blazGetCustomTableData;

        public BlazOrganizationStructureService(IHttpClientHelper httpClientHelper,
                                                OrganizationStructureTreeService.OrganizationStructureTreeServiceClient organizationStructureTreeServiceClient,
                                                IBlazGetCustomTableData<GetAllOrganizationResponseDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _organizationStructureTreeServiceClient = organizationStructureTreeServiceClient;
            _blazGetCustomTableData = blazGetCustomTableData;
        }


        // Organization Structure Operations

        public async Task<List<TreeItemResponseDto>> GetOrganizationStructureTreeByRootId(long IdCarrier)
        {
            var request = new IdCarrier
            {
                Id = IdCarrier
            };

            var organizationStructureTree = await _organizationStructureTreeServiceClient.GetOrganizationStructureTreeByRootIdAsync(request);

            return [.. organizationStructureTree.Items.Select(item => new TreeItemResponseDto
                {
                    Id = item.Id,
                    Text = item.Text,
                    ParentId = item.ParentId,
                    Description = item.Description,
                    IsActive = item.IsActive,
                    Signature = item.Signature,
                    IsLeaf = item.IsLeaf,
                    OrganizationId = item.OrganizationId,
                })];
        }

        public async Task<GetOrganizationRootResponseDto[]> GetAllOrganizationStructureRootsAsync()
        {
            var apiResponse = await _httpClientHelper.GetAsync<GetOrganizationRootResponseDto[]>("api/OrganizationStructure/GetAllOrganizationStructureRoots");

            return (GetOrganizationRootResponseDto[])apiResponse.Data ?? [];
        }

        public async Task<GetOrganizationNodeResponseDto[]> GetFlattenedOrganizationStructureAsync(long rootId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<GetOrganizationNodeResponseDto[]>($"api/OrganizationStructure/GetFlattenedOrganizationStructure?{nameof(rootId)}={rootId}");

            return (GetOrganizationNodeResponseDto[])apiResponse.Data ?? [];
        }

        public async Task<CustomTableData<GetAllOrganizationResponseDto>> GetAllOrganizationStructureAsync(PaginationSearchModel pagination)
        {
            var apiResponse = await _blazGetCustomTableData.GetCustomTableData(pagination, "api/OrganizationStructure/GetAllOrganizationStructure");

            return apiResponse;
        }

        public async Task<AddOrUpdateOrganizationStructureRootRequestDto> GetOrganizationStructureByIdAsync(long OrganizationStructureId)
        {
            var response = await _httpClientHelper.GetAsync<AddOrUpdateOrganizationStructureRootRequestDto>($"api/OrganizationStructure/GetOrganizationStructureById?{nameof(OrganizationStructureId)}={OrganizationStructureId}");

            return (AddOrUpdateOrganizationStructureRootRequestDto)response.Data;
        }

        public async Task<ApiResponse> GetOrganizationRootIdByLookupIdsAsync(List<long> lookupIds)
        {
            return await _httpClientHelper.PostAsync(lookupIds, "api/OrganizationStructure/GetOrganizationRootIdByLookupIds");
        }


        // Organization Root Operations

        public async Task<ApiResponse> AddOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto addOrganizationStructureRootDto)
        {
            return await _httpClientHelper.PostAsync(addOrganizationStructureRootDto, "api/OrganizationStructure/AddOrganizationStructureRoot");
        }

        public async Task<ApiResponse> UpdateOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto updateOrganizationStructureRootDto)
        {
            return await _httpClientHelper.PutAsync(updateOrganizationStructureRootDto, "api/OrganizationStructure/UpdateOrganizationStructureRoot");
        }

        public async Task<ApiResponse> DeleteOrganizationStructureRootAsync(long rootId)
        {
            return await _httpClientHelper.DeleteAsync($"api/OrganizationStructure/DeleteOrganizationStructureRoot?{nameof(rootId)}={rootId}");
        }


        // Organization Node Operations

        public async Task<ApiResponse> AddOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto addOrganizationStructureNodeDto)
        {
            return await _httpClientHelper.PostAsync(addOrganizationStructureNodeDto, "api/OrganizationStructure/AddOrganizationStructureNode");
        }

        public async Task<ApiResponse> UpdateOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto updateOrganizationStructureNodeDto)
        {
            return await _httpClientHelper.PutAsync(updateOrganizationStructureNodeDto, "api/OrganizationStructure/UpdateOrganizationStructureNode");
        }

        public async Task<ApiResponse> DeleteOrganizationStructureNodeAsync(long nodeId)
        {
            return await _httpClientHelper.DeleteAsync($"api/OrganizationStructure/DeleteOrganizationStructureNode?{nameof(nodeId)}={nodeId}");
        }


        // Organization Node Lookup Items Operations

        public async Task<List<GetOrganizationNodeLookupItemResponseDto>> GetNodeLookupItemsAsync(long nodeId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<List<GetOrganizationNodeLookupItemResponseDto>>($"api/OrganizationStructure/GetNodeLookupItems?{nameof(nodeId)}={nodeId}");

            return (List<GetOrganizationNodeLookupItemResponseDto>)apiResponse.Data ?? [];
        }

        public async Task<List<GetOrganizationNodeLookupItemForViewResponseDto>> GetChildLookupItemsByParentLookupItemIdAsync(long parentLookupItemId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<List<GetOrganizationNodeLookupItemForViewResponseDto>>($"api/OrganizationStructure/GetChildLookupItemsByParentLookupItemId?{nameof(parentLookupItemId)}={parentLookupItemId}");

            return (List<GetOrganizationNodeLookupItemForViewResponseDto>)apiResponse.Data ?? [];
        }

        public async Task<ApiResponse> AddNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto addOrganizationNodeLookupItemRequestDto)
        {
            return await _httpClientHelper.PostAsync(addOrganizationNodeLookupItemRequestDto, "api/OrganizationStructure/AddNodeLookupItem");
        }

        public async Task<ApiResponse> UpdateNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto updateOrganizationNodeLookupItemRequestDto)
        {
            return await _httpClientHelper.PutAsync(updateOrganizationNodeLookupItemRequestDto, "api/OrganizationStructure/UpdateNodeLookupItem");
        }

        public async Task<ApiResponse> DeleteNodeLookupItemAsync(long nodeLookupItemId)
        {
            return await _httpClientHelper.DeleteAsync($"api/OrganizationStructure/DeleteNodeLookupItem?{nameof(nodeLookupItemId)}={nodeLookupItemId}");
        }
    }
}
