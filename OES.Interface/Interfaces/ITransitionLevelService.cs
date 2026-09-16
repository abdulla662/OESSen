using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface ITransitionLevelService
    {
        Task<IApiResponse> GetAllTransitionLevel(PaginationSearchModel pagination);
        Task<IApiResponse> GetTransitionLevels();
        Task<IApiResponse> GetTransitionLevelsByProfileId(long profileId);
        Task<IApiResponse> GetTransitionLevelById(long id);
        Task<IApiResponse> AddTransitionLevel(AddTransitionLevelRequestDto _dto);
        Task<IApiResponse> UpdateTransitionLevel(GetTransitionLevelDto _updateDelta);
        Task<IApiResponse> SoftDeleteTransitionLevelAsync(long id);
    }
}
