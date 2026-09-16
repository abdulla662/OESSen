using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.DifficultyProfile
{
    public interface IBlazDifficultyProfileService
    {
        Task<CustomTableData<DifficultyProfileDto>> GetAllDifficultyProfile(PaginationSearchModel pagination);
        Task<List<ProfileDto>> GetProfiles();
        Task<DifficultyProfileDto> GetProfileById(long id);
        Task<ApiResponse> AddDifficultyProfile(AddDifficultyProfileDto _addDifficultyProfileDto);
        Task<ApiResponse> UpdateDifficultyProfile(DifficultyProfileDto _updateDifficultyProfileDto);
        Task<ApiResponse> SoftDeleteDifficultyProfile(long id);
        //Task<List<GetOESGroupDto>> GetUserDifficultyProfileGroupsAsync();
        //Task<DifficultyProfileGroupDto> GetDifficultyProfileGroupsAsync(long difficultyProfileId);
    }
}
