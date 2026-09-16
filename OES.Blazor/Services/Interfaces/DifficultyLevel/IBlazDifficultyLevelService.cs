using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.DifficultyLevel
{
    public interface IBlazDifficultyLevelService
    {
        Task<CustomTableData<DifficultyLevelDto>> PaginatedDifficultyLevel(PaginationSearchModel pagination);

        Task<List<DifficultyLevelDto>> GetAllDifficultyLevels();

        Task<List<DifficultyLevelDto>> GetDifficultyLevelByProfileIdAsync(long? profileId);

        Task<DifficultyLevelDto> GetDifficultyLevelById(long id);

        Task<List<DifficultyLevelDto>> GetDifficultyLevelsByDeltaTypeId(long DeltaTypeId);

        Task<List<GetDifficultyLevelWithQuestionCountByProfileIdDto>> GetDifficultyLevelWithQuestionCountByProfileIdAsync(long? profileId);

        Task<ApiResponse> AddDifficultyLevel(AddDifficultyLevelDto levelDto);

        Task<ApiResponse> UpdateDifficultyLevel(UpdateDifficultyLevelDto levelDto);

        Task<ApiResponse> DeleteDifficultyLevel(long id);

        //Task<List<GetOESGroupDto>> GetUserDifficultyLevelGroupsAsync();

        //Task<DifficultyLevelGroupDto> GetDifficultyLevelGroupsAsync(long difficultyLevelId);
    }
}
