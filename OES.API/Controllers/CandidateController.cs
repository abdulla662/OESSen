using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class CandidateController : OESBaseController
    {
        private readonly ICandidateService _candidateService;

        public CandidateController(ICandidateService candidateService)
        {
            _candidateService = candidateService;
        }

        [HttpPost("GetAllCandidates")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllCandidatesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _candidateService.GetAllCandidatesAsync(paginationSearchModel);
        }

        [HttpGet("GetCandidateById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCandidateByIdAsync(long candidateId)
        {
            return await _candidateService.GetCandidateByIdAsync(candidateId);
        }

        [HttpPost("GetCandidatesWithExamDateAround")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetCandidatesWithExamDateAroundAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _candidateService.GetCandidatesWithExamDateAroundAsync(paginationSearchModel);
        }

        [HttpPost("AddCandidate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddCandidateAsync(CandidateDto addCandidateRequestDto)
        {
            return await _candidateService.AddCandidateAsync(addCandidateRequestDto);
        }

        [HttpPost("AddMultipleCandidates")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddMultipleCandidates([FromForm] CandidateAndLookUpsResponseDto candidateAndLookUpsRequestDto)
        {
            return await _candidateService.AddMultipleCandidatesCaller(candidateAndLookUpsRequestDto);
        }

        [HttpPost("ValidateCandidates")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ValidateCandidates([FromForm] CandidateDataCompositeResponseDto candidateDataCompositeDto)
        {
            return await _candidateService.ValidateCandidateData(candidateDataCompositeDto);
        }

        [HttpPost("DumpImportCandidatesWithSchedulePaperAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DumpImportCandidatesWithSchedulePaperAsync(CandidatesAndSchedulePaperResponseDto candidatesAndSchedulePaperDto)
        {
            return await _candidateService.DumpImportCandidatesWithSchedulePaperCaller(candidatesAndSchedulePaperDto);
        }

        [HttpPost("AddMultipleCandidatesToSchedulePaper")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddMultipleCandidatesToSchedulePaper([FromForm] CandidateAndLookUpsAndSchedulePapersResponseDto candidateAndLookUpsAndSchedulePapersResponse)
        {
            return await _candidateService.AddMultipleCandidatesToSchedulePaperCaller(candidateAndLookUpsAndSchedulePapersResponse);
        }

        [HttpPut("UpdateCandidate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateCandidateAsync(CandidateDto updateCandidateRequestDto)
        {
            return await _candidateService.UpdateCandidateAsync(updateCandidateRequestDto);
        }

        [HttpPost("DeleteCandidate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteCandidateAsync([FromBody] long candidateId)
        {
            return await _candidateService.DeleteCandidateAsync(candidateId);
        }

        [HttpPost("AllocateCandidateByLookUpsIds")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AllocateCandidateByLookUpsIds([FromBody] AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto)
        {
            return await _candidateService.AllocateCandidateByLookUpsIdsCaller(allocateLookUpsSchedulePaperRequestDto);
        }

        [HttpPost("DownloadCandidatesUploadRelatedFiles")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DownloadCandidatesUploadRelatedFilesAsync(GetOrganizationRootResponseDto selectedRoot)
        {
            return await _candidateService.DownloadCandidatesUploadRelatedFilesAsync(selectedRoot);
        }

        [HttpGet("DownloadCandidatesWithExistingVenuesExcelFile")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DownloadCandidatesWithExistingVenuesExcelFileAsync()
        {
            return await _candidateService.DownloadCandidatesWithExistingVenuesExcelFileAsync();
        }

        [DoNotEncrypt]
        [HttpPost("ExportVerificationCodeExcel")]
        [OESFilter(Authorize = true)]
        public async Task<IActionResult> ExportVerificationCodeExcelAsync([FromBody] ExportVerificationCodeRequestDto request)
        {
            var result = await _candidateService.ExportVerificationCodeExcelAsync(request);

            return File(result.Bytes, result.ContentType, result.FileName);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserCandidateGroupsAsync")]
        //public async Task<ApiResponse> GetUserCandidateGroupsAsync()
        //{
        //    return await _candidateService.GetUserCandidateGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetCandidateGroupsAsync")]
        //public async Task<IApiResponse> GetCandidateGroupsAsync(long candidateId)
        //{
        //    return await _candidateService.GetCandidateGroupsAsync(candidateId);
        //}
    }
}