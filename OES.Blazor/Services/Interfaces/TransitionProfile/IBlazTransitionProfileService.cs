using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.TransitionProfile
{
    public interface IBlazTransitionProfileService
    {
        Task<ApiResponse> AddTransitionProfile(AddTransitionProfileDto _ProfileDto);

        Task<CustomTableData<TransitionProfileDto>> GetAllTransitionProfile(PaginationSearchModel pagination);

        Task<ApiResponse> EditTransitionProfileAsync(TransitionProfileUpdateRequestDto transitionProfileUpdateRequestDto);

        Task<List<TransitionProfileDto>> GetProfiles();

        Task<ApiResponse> SoftDeleteTransitionProfileAsync(long profileId);

        Task<TransitionProfileUpdateRequestDto> GetTransitionProfileById(long id);

        //Task<List<GetOESGroupDto>> GetTransitionProfileGroupsAsync();

        //Task<TransitionProfileDto> GetTransitionGroupsAsync(long TransitionProfileId);
    }
}
