using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface ITransitionProfileService
    {
        Task<IApiResponse> AddTransitionProfile(AddTransitionProfileDto _addProfile);

        Task<IApiResponse> GetAllTransitionProfile(PaginationSearchModel searchModel);

        Task<IApiResponse> EditTransitionProfileAsync(TransitionProfileUpdateRequestDto transitionProfileUpdateRequestDto);

        Task<IApiResponse> GetProfiles();

        Task<IApiResponse> GetTransitionProfileById(long id);

        Task<IApiResponse> SoftDeleteTransitionProfileAsync(long profileId);

        //Task<ApiResponse> GetTransitionProfileGroupsAsync();

        //Task<ApiResponse> GetTransitionGroupsAsync(long TransitionProfileId);
    }
}
