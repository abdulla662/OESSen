using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.TransitionProfile
{
    public class BlazTransitionProfileService : IBlazTransitionProfileService
    {
        private readonly IBlazGetCustomTableData<TransitionProfileDto> _blazGetCustomTable;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazTransitionProfileService(IBlazGetCustomTableData<TransitionProfileDto> blazGetCustomTable,
                                            IHttpClientHelper httpClientHelper)
        {
            _blazGetCustomTable = blazGetCustomTable;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> AddTransitionProfile(AddTransitionProfileDto _ProfileDto)
        {
            return await _httpClientHelper.PostAsync(_ProfileDto, "api/TransitionProfile/AddTransitionProfile");
        }

        public async Task<CustomTableData<TransitionProfileDto>> GetAllTransitionProfile(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTable.GetCustomTableData(pagination, "api/TransitionProfile/GetAllTransitionProfile");
        }

        public async Task<ApiResponse> EditTransitionProfileAsync(TransitionProfileUpdateRequestDto transitionProfileUpdateRequestDto)
        {
            return await _httpClientHelper.PutAsync(transitionProfileUpdateRequestDto, "api/TransitionProfile/UpdateTransitionProfile");
        }

        public async Task<List<TransitionProfileDto>> GetProfiles()
        {
            var result = await _httpClientHelper.GetAsync<List<TransitionProfileDto>>("api/TransitionProfile/GetAllProfiles");

            return (List<TransitionProfileDto>)result.Data;
        }

        public async Task<ApiResponse> SoftDeleteTransitionProfileAsync(long profileId)
        {
            return await _httpClientHelper.DeleteAsync($"api/TransitionProfile/DeleteTransitionProfile?profileId={profileId}");
        }

        public async Task<TransitionProfileUpdateRequestDto> GetTransitionProfileById(long profileId)
        {
            var response = await _httpClientHelper.GetAsync<TransitionProfileUpdateRequestDto>($"api/TransitionProfile/GetTransitionProfile?profileId={profileId}");

            return (TransitionProfileUpdateRequestDto)response.Data;
        }

        //public async Task<List<GetOESGroupDto>> GetTransitionProfileGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/TransitionProfile/GetTransitionProfileGroupsAsync");

        //    var data = (List<GetOESGroupDto>)response.Data;

        //    return data ?? [];
        //}

        //public async Task<TransitionProfileDto> GetTransitionGroupsAsync(long TransitionProfileId)
        //{
        //    var response = await _httpClientHelper.GetAsync<TransitionProfileDto>($"api/TransitionProfile/GetTransitionGroupsAsync?TransitionProfileId={TransitionProfileId}");

        //    return response.Data as TransitionProfileDto ?? new TransitionProfileDto();
        //}
    }
}