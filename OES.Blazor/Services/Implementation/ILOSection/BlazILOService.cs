using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;
using SharedHelper.General;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.ILOSection
{
    public class BlazILOService : IBlazILOService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<ILODto> _blazGetCustomTableData;


        public BlazILOService(IHttpClientHelper httpClientHelper,
                              IBlazGetCustomTableData<ILODto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }


        public async Task<List<TreeItemResponseDto>> GetChildrenByParentIdAsync(long parentId, string signature)
        {
            var dto = new TreeItemRequestDto { ParentId = parentId, Signature = signature };
            var response = await _httpClientHelper.PostAsync(dto, "api/ILO/GetByParentIdAndSignature");
            if (response?.Data is not JsonElement json) return [];
            return json.Deserialize<List<TreeItemResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }


        public async Task<CustomTableData<ILODto>> GetAllRoots(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/ILO/GetPaged_ILOS");
        }


        public async Task<ApiResponse> AddRoot(ILONewRootDto iLONewRootDto)
        {
            return await _httpClientHelper.PostAsync(iLONewRootDto, "api/ILO/AddNewRoot");
        }


        public async Task<ApiResponse> InsertNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            var apiResponse = await _httpClientHelper.PostAsync(iloRequestDto, "api/ILO/insertNode");

            return apiResponse;
        }


        public async Task<ApiResponse> EditNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            var apiResponse = await _httpClientHelper.PostAsync(iloRequestDto, "api/ILO/editNode");

            return apiResponse;
        }


        public async Task<List<ParentItemDto>> GetParentsAsync(long? parentId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<List<ParentItemDto>>($"api/ILO/getParents?{nameof(parentId)}=" + parentId);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                return (List<ParentItemDto>)apiResponse.Data;
            }

            return [];
        }


        public async Task<ApiResponse> CanSoftDeleteIloRootAsync(long id)
        {
            return await _httpClientHelper.GetAsync<ApiResponse>($"api/ILO/canSoftDeleteIlo?itemId={id}");
        }


        public async Task<ApiResponse> SoftDeleteILORoot(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/ILO/softDeleteRoot?id={id}");
        }


        public async Task<IApiResponse> SoftDeleteWithChildernILORoot(long id)
        {
            var response = await _httpClientHelper._httpClient.DeleteAsync($"{CentralizedUrlHelper.OesApiBaseUrl}api/ILO/SoftDeleteRootWithChildern?id={id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            else
            {
                return new ApiResponse
                {
                    StatusCode = response.StatusCode,
                    Data = response,
                    Message = $"Error: {response.ReasonPhrase}"
                };
            }
        }


        public async Task<ApiResponse?> EditILORoot(IloEditDTO IloEditDTO)
        {
            var response = await _httpClientHelper.PostAsync(IloEditDTO, "api/ILO/EditIloRoot");

            return response;
        }


        public async Task<ApiResponse> GetILORoot(long Id)
        {
            return await _httpClientHelper.GetAsync<ILODto>($"api/ILO/GetRootById/{Id}");
        }


        public async Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long itemId)
        {
            return await _httpClientHelper.GetAsync<ApiResponse>($"api/ILO/executeSoftDeleteForNode?itemId={itemId}");
        }


        public async Task<ApiResponse> TransferChildrenForNodeAsync(long itemId)
        {
            return await _httpClientHelper.GetAsync<ApiResponse>($"api/ILO/transferChildrenForNode?itemId={itemId}");
        }


        public async Task<List<RootIloDto>> GetRootIlosNodesAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<RootIloDto>>("api/ILO/GetRootILO");

            if (response?.Data is null) return [];

            if (response.Data is JsonElement json)
                return json.Deserialize<List<RootIloDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            return response.Data as List<RootIloDto> ?? [];
        }


        public async Task<IloGroupsDto> GetIloGroupsAsync(long iloId)
        {
            var response = await _httpClientHelper.GetAsync<IloGroupsDto>($"api/ILO/GetIloGroups?{nameof(iloId)}={iloId}");

            if (response?.Data is JsonElement json)
                return json.Deserialize<IloGroupsDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new IloGroupsDto();

            return response?.Data as IloGroupsDto ?? new IloGroupsDto();
        }


        public async Task<List<GetOESGroupDto>> GetUserIloGroupsAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/ILO/GetUserIloGroupAsync");

            if (response?.Data is null) return [];

            var data = response.Data as List<GetOESGroupDto>;

            return data ?? [];
        }
    }
}
