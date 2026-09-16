using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.General;
using Newtonsoft.Json;

namespace OES.Blazor.Services.Implementation
{
    public class BlazQueueSuspendService : IBlazQueueSuspendService
    {
        private readonly IHttpClientHelper _httpClient;
        private readonly IBlazGetCustomTableData<QueueSuspendResponseDto> _blazGetCustomTableData;
        private const string ApiRelativeUrl = "api/QueueSuspend";

        public BlazQueueSuspendService(
            IHttpClientHelper httpClient,
            IBlazGetCustomTableData<QueueSuspendResponseDto> blazGetCustomTableData
        )
        {
            _httpClient = httpClient;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<ApiResponse> ManagePaperSuspensionsAsync(ManagePaperSuspensionsDto requestDto)
        {
            return await _httpClient.PostAsync(requestDto, $"{ApiRelativeUrl}/ManagePaperSuspensions");
        }

        public async Task<CustomTableData<QueueSuspendResponseDto>> GetPaginatedPaperVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            return await _blazGetCustomTableData
                .GetCustomTableData(paginationSearchModel, $"{ApiRelativeUrl}/GetPaginatedPaperVenuesForSuspension?{nameof(paperId)}={paperId}");
        }

        public async Task<List<string>> GetSuspendedVenueCodesByPaperIdAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<List<string>>($"{ApiRelativeUrl}/GetSuspendedVenueCodesByPaperId?{nameof(paperId)}={paperId}");

            if (response?.Data == null)
            {
                return [];
            }

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(response.Data.ToString() ?? "[]") ?? [];
            }
            catch
            {
                return response.Data as List<string> ?? [];
            }
        }

        public async Task<ApiResponse> ManageFormSuspensionsAsync(ManageFormSuspensionsDto requestDto)
        {
            return await _httpClient.PostAsync(requestDto, $"{ApiRelativeUrl}/ManageFormSuspensions");
        }

        public async Task<CustomTableData<QueueSuspendResponseDto>> GetPaginatedFormVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long formId)
        {
            return await _blazGetCustomTableData
                .GetCustomTableData(paginationSearchModel, $"{ApiRelativeUrl}/GetPaginatedFormVenuesForSuspension?{nameof(formId)}={formId}");
        }

        public async Task<List<string>> GetSuspendedVenueCodesByFormIdAsync(long formId)
        {
            var response = await _httpClient.GetAsync<List<string>>($"{ApiRelativeUrl}/GetSuspendedVenueCodesByFormId?{nameof(formId)}={formId}");

            if (response?.Data == null)
            {
                return [];
            }

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(response.Data.ToString() ?? "[]") ?? [];
            }
            catch
            {
                return response.Data as List<string> ?? [];
            }
        }
    }
}