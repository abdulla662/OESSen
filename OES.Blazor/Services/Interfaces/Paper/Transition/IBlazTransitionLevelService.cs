using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.Dtos.Paper.TransitionDtos.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Paper.Transition
{
    public interface IBlazTransitionLevelService
    {
        Task<CustomTableData<TransitionLevelDto>> GetAllTransitionLevel(PaginationSearchModel pagination);
        Task<List<GetTransitionLevelsDto>> GetTransitionLevels();
        Task<GetTransitionLevelDto> GetTransitionLevelById(long id);
        Task<List<GetTransitionLevelsDto>> GetTransitionLevelsByProfileId(long profileId);
        Task<ApiResponse> AddTransitionLevel(AddTransitionLevelRequestDto addTransitionLevel);
        Task<ApiResponse> UpdateTransitionLevel(GetTransitionLevelDto updateTransitionLevel);
        Task<ApiResponse> SoftDeleteTransitionLevel(long id);
    }
}
