using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IDisabilityService
    {
        Task<ApiResponse> GetAllDisabilitiesForPaginationAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetAllDisabilitiesAsync();
        Task<ApiResponse> GetDisabilityByIdAsync(long id);
        Task<ApiResponse> UpdateDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisability);
        Task<ApiResponse> DeleteDisabilityAsync(long id);
        Task<ApiResponse> AddDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisabilityDto);
        Task<ApiResponse> AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto addCandidateExtraTimeDto);
    }
}
