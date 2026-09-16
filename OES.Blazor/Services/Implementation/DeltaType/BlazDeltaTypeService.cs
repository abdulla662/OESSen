using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.General;
using System.Net.Http.Json;

namespace OES.Blazor.Services.Implementation.DeltaType
{
    public class BlazDeltaTypeService : IBlazDeltaTypeService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<GetDeltaTypeDto> _blazGetCustomTableData;

        public BlazDeltaTypeService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<GetDeltaTypeDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<List<GetDeltaTypeDto>> GetDeltaTypes()
        {
            var result = await _httpClientHelper.GetAsync<List<GetDeltaTypeDto>>("api/DeltaType/GetDeltaTypes");

            return (List<GetDeltaTypeDto>)result.Data;
        }

        public async Task<CustomTableData<GetDeltaTypeDto>> GetAllDeltaTypesPaginated(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/DeltaType/GetDeltaTypesPaginated");
        }

        public async Task<GetDeltaTypeDto> GetDeltaTypeById(long id)
        {
            var result = await _httpClientHelper.GetAsync<GetDeltaTypeDto>($"api/DeltaType/GetDeltaTypeById/{id}");
            return (GetDeltaTypeDto)result.Data;
        }

        public async Task<ApiResponse> AddDeltaType(AddDeltaTypeDto addDeltaType)
        {
            return await _httpClientHelper.PostAsync(addDeltaType, "api/DeltaType/AddDeltaType");
        }

        public async Task<ApiResponse> UpdateDeltaType(GetDeltaTypeDto updateDeltaType)
        {
            return await _httpClientHelper.PutAsync(updateDeltaType, "api/DeltaType/UpdateDeltaType");
        }

        public async Task<ApiResponse> SoftDeleteDeltaType(long id)
        {
            var response = await _httpClientHelper._httpClient.DeleteAsync($"api/DeltaType/SoftDeleteDeltaType/{id}");

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

        //public async Task<List<GetOESGroupDto>> GetUserDeltaTypeGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/DeltaType/GetUserDeltaTypeGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<DeltaTypeGroupDto> GetDeltaTypeGroupsAsync(long deltaTypeId)
        //{
        //    var response = await _httpClientHelper.GetAsync<DeltaTypeGroupDto>($"api/DeltaType/GetDeltaTypeGroupsAsync?deltaTypeId={deltaTypeId}");
        //    return response.Data as DeltaTypeGroupDto ?? new DeltaTypeGroupDto();
        //}
    }
}
