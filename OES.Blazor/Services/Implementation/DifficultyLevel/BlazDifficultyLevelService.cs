using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.DifficultyLevel
{
    public class BlazDifficultyLevelService(IBlazGetCustomTableData<DifficultyLevelDto> _blazGetCustomTableData, IHttpClientHelper _httpClientHelper) : IBlazDifficultyLevelService
    {
        public async Task<CustomTableData<DifficultyLevelDto>> PaginatedDifficultyLevel(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/DifficultyLevel/PaginatedDifficultyLevel");
        }

        public async Task<List<DifficultyLevelDto>> GetAllDifficultyLevels()
        {
            var response = await _httpClientHelper.GetAsync<List<DifficultyLevelDto>>("api/DifficultyLevel/GetAllDifficultyLevels");

            return (List<DifficultyLevelDto>)response.Data ?? [];
        }

        public async Task<DifficultyLevelDto> GetDifficultyLevelById(long id)
        {
            var response = await _httpClientHelper.GetAsync<DifficultyLevelDto>($"api/DifficultyLevel/GetDifficultyLevelById?id={id}");

            return (DifficultyLevelDto)response.Data;
        }

        public async Task<List<GetDifficultyLevelWithQuestionCountByProfileIdDto>> GetDifficultyLevelWithQuestionCountByProfileIdAsync(long? profileId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetDifficultyLevelWithQuestionCountByProfileIdDto>>(
                $"api/DifficultyLevel/GetDifficultyLevelWithQuestionCountByProfileId?profileId={profileId}"
            );

            return (List<GetDifficultyLevelWithQuestionCountByProfileIdDto>)response.Data ?? [];
        }

        public async Task<List<DifficultyLevelDto>> GetDifficultyLevelByProfileIdAsync(long? profileId)
        {
            var response = await _httpClientHelper.GetAsync<List<DifficultyLevelDto>>($"api/DifficultyLevel/GetDifficultyLevelByProfileId?profileId={profileId}");

            return (List<DifficultyLevelDto>)response.Data ?? [];
        }

        public async Task<List<DifficultyLevelDto>> GetDifficultyLevelsByDeltaTypeId(long DeltaTypeId)
        {
            var response = await _httpClientHelper.GetAsync<List<DifficultyLevelDto>>($"api/DifficultyLevel/GetDifficultyLevelsByDeltaTypeId?DeltaTypeId={DeltaTypeId}");

            return (List<DifficultyLevelDto>)response.Data ?? [];
        }

        public Task<ApiResponse> AddDifficultyLevel(AddDifficultyLevelDto levelDto)
        {
            return _httpClientHelper.PostAsync(levelDto, "api/DifficultyLevel/AddDifficultyLevelAsync");
        }

        public async Task<ApiResponse> UpdateDifficultyLevel(UpdateDifficultyLevelDto updateDto)
        {
            return await _httpClientHelper.PutAsync(updateDto, "api/DifficultyLevel/UpdateDifficultyLevel");
        }

        public Task<ApiResponse> DeleteDifficultyLevel(long id)
        {
            return _httpClientHelper.PostAsync(id, "api/DifficultyLevel/DeleteDifficultyLevel");
        }

        //public async Task<List<GetOESGroupDto>> GetUserDifficultyLevelGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/DifficultyLevel/GetUserDifficultyLevelGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<DifficultyLevelGroupDto> GetDifficultyLevelGroupsAsync(long difficultyLevelId)
        //{
        //    var response = await _httpClientHelper.GetAsync<DifficultyLevelGroupDto>($"api/DifficultyLevel/GetDifficultyLevelGroupsAsync?difficultyLevelId={difficultyLevelId}");
        //    return response.Data as DifficultyLevelGroupDto ?? new DifficultyLevelGroupDto();
        //}
    }
}
