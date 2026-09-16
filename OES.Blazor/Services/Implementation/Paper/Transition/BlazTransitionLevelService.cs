using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.Dtos.Paper.TransitionDtos.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Paper.Transition
{
    public class BlazTransitionLevelService : IBlazTransitionLevelService
    {
        private readonly IBlazGetCustomTableData<TransitionLevelDto> _blazGetCustomTable;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazTransitionLevelService(IBlazGetCustomTableData<TransitionLevelDto> blazGetCustomTable,
                                          IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTable = blazGetCustomTable;
            _httpClientHelper = httpClientHelper;
        }

        public Task<CustomTableData<TransitionLevelDto>> GetAllTransitionLevel(PaginationSearchModel pagination)
        {
            return _blazGetCustomTable.GetCustomTableData(pagination, "api/TransitionLevel/GetAllTransitionLevel");
        }

        public async Task<List<GetTransitionLevelsDto>> GetTransitionLevels()
        {
            var result = await _httpClientHelper.GetAsync<List<GetTransitionLevelsDto>>("api/TransitionLevel/GetTransitionLevels");

            return (List<GetTransitionLevelsDto>)result.Data ?? [];
        }

        public async Task<ApiResponse> AddTransitionLevel(AddTransitionLevelRequestDto addTransitionLevel)
        {
            return await _httpClientHelper.PostAsync(addTransitionLevel, "api/TransitionLevel/AddTransitionLevel");
        }

        public async Task<ApiResponse> UpdateTransitionLevel(GetTransitionLevelDto updateTransitionLevel)
        {
            return await _httpClientHelper.PutAsync(updateTransitionLevel, "api/TransitionLevel/UpdateTransitionLevel");
        }

        public async Task<ApiResponse> SoftDeleteTransitionLevel(long id)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/TransitionLevel/SoftDeleteTransitionLevelAsync?id={id}");

            return response;
        }

        public async Task<GetTransitionLevelDto> GetTransitionLevelById(long id)
        {
            var response = await _httpClientHelper.GetAsync<GetTransitionLevelDto>($"api/TransitionLevel/GetTransitionLevelById?id={id}");

            return (GetTransitionLevelDto)response.Data ?? new();
        }

        public async Task<List<GetTransitionLevelsDto>> GetTransitionLevelsByProfileId(long profileId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetTransitionLevelsDto>>($"api/TransitionLevel/GetTransitionLevelsByProfileId?profileId={profileId}");

            return (List<GetTransitionLevelsDto>)response.Data ?? [];
        }
    }
}
