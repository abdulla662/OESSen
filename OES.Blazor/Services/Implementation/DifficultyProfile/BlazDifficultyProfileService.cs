using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.DifficultyProfile
{
    public class BlazDifficultyProfileService : IBlazDifficultyProfileService
    {
        private readonly IBlazGetCustomTableData<DifficultyProfileDto> _blazGetCustomTable;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazDifficultyProfileService(IBlazGetCustomTableData<DifficultyProfileDto> blazGetCustomTable,
                                            IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTable = blazGetCustomTable;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<CustomTableData<DifficultyProfileDto>> GetAllDifficultyProfile(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTable.GetCustomTableData(pagination, "api/DifficultyProfile/GetAllDifficultyProfile");
        }

        public async Task<List<ProfileDto>> GetProfiles()
        {
            var result = await _httpClientHelper.GetAsync<List<ProfileDto>>("api/DifficultyProfile/GetProfiles");

            return (List<ProfileDto>)result.Data ?? [];
        }

        public async Task<DifficultyProfileDto> GetProfileById(long id)
        {
            var response = await _httpClientHelper.GetAsync<DifficultyProfileDto>($"api/DifficultyProfile/GetProfileById?id={id}");

            return (DifficultyProfileDto)response.Data;
        }

        public async Task<ApiResponse> AddDifficultyProfile(AddDifficultyProfileDto _addDifficultyProfileDto)
        {
            return await _httpClientHelper.PostAsync(_addDifficultyProfileDto, "api/DifficultyProfile/AddDifficultyProfile");
        }

        public async Task<ApiResponse> UpdateDifficultyProfile(DifficultyProfileDto _updateDifficultyProfileDto)
        {
            return await _httpClientHelper.PutAsync(_updateDifficultyProfileDto, "api/DifficultyProfile/UpdateDifficultyProfile");
        }

        public async Task<ApiResponse> SoftDeleteDifficultyProfile(long id)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/DifficultyProfile/SoftDeleteDifficultyProfile?{nameof(id)}={id}");

            return response;
        }

        //public async Task<List<GetOESGroupDto>> GetUserDifficultyProfileGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/DifficultyProfile/GetUserDifficultyProfileGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<DifficultyProfileGroupDto> GetDifficultyProfileGroupsAsync(long difficultyProfileId)
        //{
        //    var response = await _httpClientHelper.GetAsync<DifficultyProfileGroupDto>($"api/DifficultyProfile/GetDifficultyProfileGroupsAsync?difficultyProfileId={difficultyProfileId}");
        //    return response.Data as DifficultyProfileGroupDto ?? new DifficultyProfileGroupDto();
        //}
    }
}
