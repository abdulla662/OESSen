using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Disability
{
    public class BlazDisabilityService : IBlazDisabilityService
    {
        private readonly IBlazGetCustomTableData<AddOrUpdateDisabilityDto> _blazGetCustomTable;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazDisabilityService(IBlazGetCustomTableData<AddOrUpdateDisabilityDto> blazGetCustomTable, IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTable = blazGetCustomTable;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> AddDisability(AddOrUpdateDisabilityDto addOrUpdateDisabilityDto)
        {
            return await _httpClientHelper.PostAsync(addOrUpdateDisabilityDto, "api/Disability/AddDisability");
        }

        public async Task<ApiResponse> AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto addCandidateExtraTimeDto)
        {
            return await _httpClientHelper.PostAsync(addCandidateExtraTimeDto, "api/Disability/AddCandidateExtraTime");
        }

        public async Task<ApiResponse> DeleteDisability(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/Disability/DeleteDisability?{nameof(id)}={id}");
        }

        public async Task<List<AddOrUpdateDisabilityDto>> GetAllDisabilities()
        {
            var response = await _httpClientHelper.GetAsync<List<AddOrUpdateDisabilityDto>>("api/Disability/GetAllDisabilities");
            return (List<AddOrUpdateDisabilityDto>)response.Data ?? [];
        }

        public async Task<CustomTableData<AddOrUpdateDisabilityDto>> GetAllDisabilitiesForPagination(PaginationSearchModel paginationSearchModel)
        {
            return await _blazGetCustomTable.GetCustomTableData(paginationSearchModel, "api/Disability/GetAllDisabilities");
        }

        public async Task<ApiResponse> GetDisabilityById(long id)
        {
            return await _httpClientHelper.GetAsync<AddOrUpdateDisabilityDto>($"api/Disability/GetDisabilityById?{nameof(id)}={id}");
        }

        public async Task<ApiResponse> UpdateDisability(AddOrUpdateDisabilityDto addOrUpdateDisability)
        {
            return await _httpClientHelper.PutAsync(addOrUpdateDisability, "api/Disability/UpdateDisability");
        }
    }
}
