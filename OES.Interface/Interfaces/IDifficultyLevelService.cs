using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IDifficultyLevelService
    {
        Task<ApiResponse> GetAllDifficultyLevels();

        Task<ApiResponse> GetAllDifficultyLevelsByProfileIdAsync(long? profileId);

        Task<ApiResponse> GetDifficultyLevelWithQuestionCountByProfileIdAsync(long? profileId);

        Task<IApiResponse> GetAllDifficultyLevel(PaginationSearchModel pagination);

        Task<ApiResponse> AddDifficultyLevelAsync(AddDifficultyLevelDto levelDto);

        Task<ApiResponse> GetDifficultyLevelByIdAsync(long id);

        Task<IApiResponse> GetDifficultyLevelsByDeltaTypeId(long DeltaTypeId);

        Task<ApiResponse> UpdateDifficultyLevelAsync(UpdateDifficultyLevelDto updateDto);

        Task<ApiResponse> DeleteDifficultyLevelAsync(long id);

        //Task<ApiResponse> GetUserDifficultyLevelGroupsAsync();

        //Task<ApiResponse> GetDifficultyLevelGroupsAsync(long difficultyLevelId);
    }
}
