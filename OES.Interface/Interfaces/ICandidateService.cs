using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ICandidateService
    {
        Task<ExcelFileResult> ExportVerificationCodeExcelAsync(ExportVerificationCodeRequestDto request);

        Task<ApiResponse> GetAllCandidatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetCandidatesWithExamDateAroundAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetCandidateByIdAsync(long candidateId);

        Task<ApiResponse> AddCandidateAsync(CandidateDto addCandidateRequestDto);

        Task<ApiResponse> AddMultipleCandidatesCaller(CandidateAndLookUpsResponseDto candidateAndLookUpsResponseDto);

        Task<ApiResponse> ValidateCandidateData(CandidateDataCompositeResponseDto candidateDataCompositeResponseDto);

        Task<ApiResponse> DumpImportCandidatesWithSchedulePaperCaller(CandidatesAndSchedulePaperResponseDto candidatesAndSchedulePaperResponseDto);

        Task<ApiResponse> AddMultipleCandidatesToSchedulePaperCaller(CandidateAndLookUpsAndSchedulePapersResponseDto candidateAndLookUpsAndSchedulePapersResponse);

        Task<ApiResponse> UpdateCandidateAsync(CandidateDto updateCandidateRequestDto);

        Task<ApiResponse> DeleteCandidateAsync(long candidateId);

        Task<ApiResponse> AllocateCandidateByLookUpsIdsCaller(AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto);

        Task<ApiResponse> DownloadCandidatesUploadRelatedFilesAsync(GetOrganizationRootResponseDto selectedRoot);

        Task<ApiResponse> DownloadCandidatesWithExistingVenuesExcelFileAsync();

        Task ProcessCandidateImportToSchedulePaperAsync(Guid candidatesKey, long SchedulePaperId, long VenueId, long batchId, FilterParamsValues filterParamsValues);

        //Task<ApiResponse> GetUserCandidateGroupsAsync();

        //Task<ApiResponse> GetCandidateGroupsAsync(long candidateId);
    }
}