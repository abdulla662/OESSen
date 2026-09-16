using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.ItemBank
{
    public class BlazItemBankService : IBlazItemBankService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<ItemBankTemplateDto> _aiBlazGetCustomTableData;
        private readonly IBlazGetCustomTableData<NewItemBankDTO> _blazGetCustomTableData;

        public BlazItemBankService(IHttpClientHelper httpClientHelper,
                                   IBlazGetCustomTableData<ItemBankTemplateDto> aiBlazGetCustomTableData,
                                   IBlazGetCustomTableData<NewItemBankDTO> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _aiBlazGetCustomTableData = aiBlazGetCustomTableData;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<ApiResponse> UpdateItemBank(NewItemBankDTO itemBankDTO)
        {
            var response = await _httpClientHelper.PostAsync(itemBankDTO, "api/ItemBank/UpdateItemBank");

            return response;
        }

        public async Task<EditItemBankNodeDto> GetEditItemBank(long Id)
        {
            var apiResposne = await _httpClientHelper.GetAsync<EditItemBankNodeDto>($"api/ItemBank/GetEditNodeObject?id={Id}");

            return (EditItemBankNodeDto)apiResposne.Data;
        }

        public async Task<CustomTableData<NewItemBankDTO>> GetAllItemBankAsync(PaginationSearchModel paginationSearchModel)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, "api/ItemBank/GetAllAsync");

            return response;
        }

        public async Task<NewItemBankDTO> GetItemBankByID(long Id)
        {
            var response = await _httpClientHelper.GetAsync<NewItemBankDTO>($"api/ItemBank/GetItemBankByID?Id={Id}");

            return (NewItemBankDTO)response.Data;
        }

        public async Task<List<ItemParentList>> GetItemParentLists(long? Id)
        {
            var ApiResposne = await _httpClientHelper.GetAsync<List<ItemParentList>>("api/ItemBank/GetParentsForEditNodeObject?id=" + Id);

            return (List<ItemParentList>)ApiResposne.Data ?? [];
        }

        public async Task<ApiResponse> TransferQuestionsToItemBankAsync(TransferQuestionsToItemBankDto dto)
        {
            return await _httpClientHelper.PostAsync(dto, "api/ItemBank/TransferQuestionsToItemBank");
        }

        public async Task<ApiResponse> UpdateNode(EditItemBankNodeDto editItemBankNodeDto)
        {
            return await _httpClientHelper.PostAsync(editItemBankNodeDto, "api/ItemBank/EditNode");
        }

        public async Task<ApiResponse> AddItemBankAsync(NewItemBankDTO itemBankDto)
        {
            var Response = await _httpClientHelper.PostAsync(itemBankDto, "api/ItemBank/add");

            return Response;
        }

        public async Task<ApiResponse> CanSoftDeleteItemBankAsync(long itemId)
        {
            var response = await _httpClientHelper.GetAsync<ApiResponse>($"api/ItemBank/canSoftDeleteItemBank?itemId={itemId}");

            return response;
        }

        public async Task ExecuteSoftDeleteForNodeAsync(long itemId)
        {
            await _httpClientHelper.GetAsync<ApiResponse>($"api/ItemBank/executeSoftDeleteForNode?itemId={itemId}");
        }

        public async Task<ApiResponse> ExecuteSoftDeleteForRootAsync(long itemId)
        {
            var response = await _httpClientHelper.GetAsync<ApiResponse>($"api/ItemBank/executeSoftDeleteForRoot?{nameof(itemId)}={itemId}");

            return response;
        }

        public async Task TransferChildrenForNodeAsync(long itemId)
        {
            await _httpClientHelper.GetAsync<ApiResponse>($"api/ItemBank/transferChildrenForNode?itemId={itemId}");
        }

        public async Task<ApiResponse> AddNewNode(ItemBankNode itemBankNode)
        {
            return await _httpClientHelper.PostAsync(itemBankNode, "api/ItemBank/AddNode");
        }

        public async Task<ItemBankLevelValidation> GetNodeValidation(long ParentId)
        {
            var result = await _httpClientHelper.GetAsync<ItemBankLevelValidation>($"api/ItemBank/ValidateNode?ParentId={ParentId}");

            return (ItemBankLevelValidation)result.Data;
        }

        public async Task<List<RootItemBankDto>> GetRootItemBanksNodesAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<RootItemBankDto>>("api/ItemBank/GetRootItemBanks");

            var data = (List<RootItemBankDto>)response.Data ?? [];

            return data;
        }

        public async Task<ItemBankGroupsDto> GetItemBankGroupsAsync(long itemBankId)
        {
            var response = await _httpClientHelper.GetAsync<ItemBankGroupsDto>($"api/ItemBank/GetItemBankGroups?{nameof(itemBankId)}={itemBankId}");

            if (response?.Data is JsonElement json)
                return json.Deserialize<ItemBankGroupsDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ItemBankGroupsDto();

            return response?.Data as ItemBankGroupsDto ?? new ItemBankGroupsDto();
        }

        public async Task<List<RootItemBankDto>> GetItemBanksListAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<RootItemBankDto>>("api/ItemBank/GetItemBanksList");

            var data = (List<RootItemBankDto>)response.Data;

            return data.Count > 0 ? data : [];

        }

        public async Task<ItemBankStatisticsDto> GetItemBankStatistics(long itemBankId)
        {
            var response = await _httpClientHelper.GetAsync<ItemBankStatisticsDto>($"api/ItemBank/GetItemBankStatistics?itemBankId={itemBankId}");

            return (ItemBankStatisticsDto)response.Data;
        }

        public async Task<List<UserItemBankDto>> GetAllItemBankForUserAsync(long itemBankId)
        {
            var response = await _httpClientHelper.GetAsync<List<UserItemBankDto>>($"api/ItemBank/GetAllItemBankForUserAsync?itemBankId={itemBankId}");

            return (List<UserItemBankDto>)response.Data ?? [];
        }

        public async Task<CustomTableData<NewItemBankDTO>> GetAllItemBankRootsForPaperAsync(PaginationSearchModel pagination)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(pagination, "api/ItemBank/GetAllItemBankRootsForPaperAsync");

            return response;
        }

        public async Task<List<GetOESGroupDto>> GetUserItemBankGroupsAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/ItemBank/GetUserItemBankGroupsAsync");

            var data = (List<GetOESGroupDto>)response.Data;

            return data ?? [];
        }

        public async Task<List<TreeItemResponseDto>> GetChildrenByParentIdAsync(long parentId, string signature)
        {
            var dto = new TreeItemRequestDto { ParentId = parentId, Signature = signature };
            var response = await _httpClientHelper.PostAsync(dto, "api/ItemBank/getByParentIdAndSignature");
            if (response?.Data is not JsonElement json) return [];
            return json.Deserialize<List<TreeItemResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }

        public async Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole)
        {
            var response = await _httpClientHelper.GetAsync<ApiResponse>(
                $"api/ItemBank/CanDoQuestionAction?itemBankId={itemBankId}&requiredRole={requiredRole}"
            );

            return response;
        }

        public async Task<ApiResponse> AddTemplateAIItemBankAsync(AIItemBankTemplateCreationDto dto)
        {
            var response = await _httpClientHelper.PostAsync(dto, "api/ItemBank/AddTemplateAIItemBank");

            return response;
        }

        public async Task<CustomTableData<ItemBankTemplateDto>> GetAllItemBankTemplateAsync(ItemBankPaginationSearchRequest paginationSearch)
        {
            var url = $"api/ItemBank/GetAllItemBankTemplates?isFromItemBankAI={paginationSearch.IsFromItemBankAI}";

            var result = await _aiBlazGetCustomTableData.GetCustomTableData(paginationSearch.PaginationSearch, url);

            return result;
        }

        public async Task<ApiResponse> DeleteItemBankTemplateAsync(long templateId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/ItemBank/DeleteItemBankTemplate?templateId={templateId}");

            return response;
        }

        public async Task<ApiResponse> GetItemBankTemplateById(long itemBankTemplateId)
        {
            var result = await _httpClientHelper.GetAsync<GetAIItemBankTemplateResponseDto>($"api/ItemBank/GetItemBankTemplateById?templateId={itemBankTemplateId}");

            return result;
        }
    }
}
