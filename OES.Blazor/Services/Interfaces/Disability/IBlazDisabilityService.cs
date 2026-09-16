using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Disability
{
    public interface IBlazDisabilityService
    {
        Task<CustomTableData<AddOrUpdateDisabilityDto>> GetAllDisabilitiesForPagination(PaginationSearchModel paginationSearchModel);
        Task<List<AddOrUpdateDisabilityDto>> GetAllDisabilities();
        Task<ApiResponse> GetDisabilityById(long id);
        Task<ApiResponse> UpdateDisability(AddOrUpdateDisabilityDto addOrUpdateDisability);
        Task<ApiResponse> DeleteDisability(long id);
        Task<ApiResponse> AddDisability(AddOrUpdateDisabilityDto addOrUpdateDisabilityDto);
        Task<ApiResponse> AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto addCandidateExtraTimeDto);
    }
}
