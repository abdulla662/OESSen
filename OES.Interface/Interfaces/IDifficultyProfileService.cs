using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IDifficultyProfileService
    {
        Task<IApiResponse> GetAllDifficultyProfile(PaginationSearchModel searchModel);
        Task<IApiResponse> GetProfiles();
        Task<IApiResponse> GetProfileById(long id);
        Task<IApiResponse> AddDifficultyProfile(AddDifficultyProfileDto _addDifficultyProfile);
        Task<IApiResponse> UpdateDifficultyProfile(DifficultyProfileDto _updateDifficultyProfile);
        Task<IApiResponse> SoftDeleteDifficultyProfileAsync(long id);
        //Task<ApiResponse> GetUserDifficultyProfileGroupsAsync();
        //Task<ApiResponse> GetDifficultyProfileGroupsAsync(long difficultyProfileId);
    }
}
