using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Candidate
{
    public interface IBlazCandidateService
    {
        Task<CustomTableData<CandidatesListResponseDto>> GetAllCandidatesAsync(PaginationSearchModel pagination);

        Task<CustomTableData<CandidatesListResponseDto>> GetCandidatesWithExamDateAroundAsync(PaginationSearchModel paginationSearchModel);

        Task<AddOrUpdateCandidateRequestDto> GetCandidateByIdAsync(long candidateId);

        Task<ApiResponse> AddCandidateAsync(AddOrUpdateCandidateRequestDto addCandidateRequestDto, IBrowserFile photoFile, IBrowserFile signatureFile);

        Task<ApiResponse> UpdateCandidateAsync(AddOrUpdateCandidateRequestDto updateCandidateRequestDto, IBrowserFile photoFile, IBrowserFile signatureFile);

        Task<ApiResponse> AddMultipleCandidateAsync(CandidateAndLookUpsRequestDto candidateAndLookUpsRequestDto);

        Task<ApiResponse> DeleteCandidate(long candidateId);

        Task<ApiResponse> DownloadCandidateTemplate(GetOrganizationRootResponseDto selectedRoot);

        Task<ApiResponse> AddMultipleCandidateAndSchedulePaperAsync(CandidateAndLookUpsAndSchedulePapersRequestDto candidateAndLookUpsRequestDto);

        Task<ApiResponse> ValidateCandidatesDataAsync(CandidateDataCompositeRequestDto candidateDataCompositeRequestDto);

        Task<ApiResponse> DumpImportCandidatesWithSchedulePaperAsync(DumpImportCandidatesWithSchedulePaperRequestDto dumpImportCandidatesWithSchedulePaperRequestDto);

        Task<ApiResponse> AllocateCandidateByLookUpsIds(AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto);

        Task<ApiResponse> DownloadCandidatesWithExistingVenuesExcelFileAsync();

        //Task<List<GetOESGroupDto>> GetUserCandidateGroupsAsync();

        //Task<CandidateGroupDto> GetCandidateGroupsAsync(long candidateId);
    }
}