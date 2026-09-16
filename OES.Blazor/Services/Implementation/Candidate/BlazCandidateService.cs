using Microsoft.AspNetCore.Components.Forms;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Net.Http.Json;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.Candidate
{
    public class BlazCandidateService : IBlazCandidateService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IBlazGetCustomTableData<CandidatesListResponseDto> _blazGetCustomTableData;

        public BlazCandidateService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<CandidatesListResponseDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<CustomTableData<CandidatesListResponseDto>> GetAllCandidatesAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/Candidate/GetAllCandidates");
        }

        public async Task<CustomTableData<CandidatesListResponseDto>> GetCandidatesWithExamDateAroundAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _blazGetCustomTableData.GetCustomTableData(paginationSearchModel, "api/Candidate/GetCandidatesWithExamDateAround");
        }

        public async Task<AddOrUpdateCandidateRequestDto> GetCandidateByIdAsync(long candidateId)
        {
            var response = await _httpClientHelper.GetAsync<AddOrUpdateCandidateRequestDto>($"api/Candidate/GetCandidateById?candidateId={candidateId}");

            return (AddOrUpdateCandidateRequestDto)response.Data;
        }

        public async Task<ApiResponse> AddCandidateAsync(AddOrUpdateCandidateRequestDto addCandidateRequestDto, IBrowserFile photoFile, IBrowserFile signatureFile)
        {
            using var formData = new MultipartFormDataContent
            {
                { new StringContent(addCandidateRequestDto.CandidateCode), nameof(AddOrUpdateCandidateRequestDto.CandidateCode) },
                { new StringContent(addCandidateRequestDto.Name), nameof(AddOrUpdateCandidateRequestDto.Name) },
                { new StringContent(addCandidateRequestDto.NationalId), nameof(AddOrUpdateCandidateRequestDto.NationalId) },
                { new StringContent(addCandidateRequestDto.UserName), nameof(AddOrUpdateCandidateRequestDto.UserName) },
                { new StringContent(addCandidateRequestDto.Password), nameof(AddOrUpdateCandidateRequestDto.Password) },
                { new StringContent(addCandidateRequestDto.Qualification ?? ""), nameof(AddOrUpdateCandidateRequestDto.Qualification) },
                { new StringContent(addCandidateRequestDto.DateOfBirth?.ToString() ?? ""), nameof(AddOrUpdateCandidateRequestDto.DateOfBirth) },
                { new StringContent(addCandidateRequestDto.Address ?? ""), nameof(AddOrUpdateCandidateRequestDto.Address) },
                { new StringContent(addCandidateRequestDto.Mobile), nameof(AddOrUpdateCandidateRequestDto.Mobile) },
                { new StringContent(addCandidateRequestDto.Email), nameof(AddOrUpdateCandidateRequestDto.Email) },
                { new StringContent(addCandidateRequestDto.Gender.ToString()), nameof(AddOrUpdateCandidateRequestDto.Gender) },
                { new StringContent(addCandidateRequestDto.RegistrationCenterCode ?? ""), nameof(AddOrUpdateCandidateRequestDto.RegistrationCenterCode) },
                { new StringContent(addCandidateRequestDto.RegistrationDateTime.ToString("o") ?? ""), nameof(AddOrUpdateCandidateRequestDto.RegistrationDateTime) },
                { new StringContent(addCandidateRequestDto.HasDisability.ToString()), nameof(AddOrUpdateCandidateRequestDto.HasDisability) },
                { new StringContent(addCandidateRequestDto.DisabilityId.ToString()), nameof(AddOrUpdateCandidateRequestDto.DisabilityId) }
            };

            if (photoFile != null)
            {
                var photoStream = photoFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);

                formData.Add(new StreamContent(photoStream), nameof(AddOrUpdateCandidateRequestDto.PhotoFile), photoFile.Name);
            }

            if (signatureFile != null)
            {
                var signatureStream = signatureFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);

                formData.Add(new StreamContent(signatureStream), nameof(AddOrUpdateCandidateRequestDto.SignatureFile), signatureFile.Name);
            }

            var httpClient = _httpClientHelper._httpClient;

            var response = await httpClient.PostAsync("/api/Candidate/AddCandidate", formData);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
            }

            var error = await response.Content.ReadAsStringAsync();

            return new ApiResponse { StatusCode = response.StatusCode, CustomCodeStatus = CustomCodeStatus.Failure, Message = error };
        }

        public async Task<ApiResponse> UpdateCandidateAsync(AddOrUpdateCandidateRequestDto updateCandidateRequestDto, IBrowserFile photoFile, IBrowserFile signatureFile)
        {
            using var formData = new MultipartFormDataContent
            {
                { new StringContent(updateCandidateRequestDto.CandidateId.ToString()), nameof(AddOrUpdateCandidateRequestDto.CandidateId) },
                { new StringContent(updateCandidateRequestDto.CandidateCode), nameof(AddOrUpdateCandidateRequestDto.CandidateCode) },
                { new StringContent(updateCandidateRequestDto.Name), nameof(AddOrUpdateCandidateRequestDto.Name) },
                { new StringContent(updateCandidateRequestDto.NationalId), nameof(AddOrUpdateCandidateRequestDto.NationalId) },
                { new StringContent(updateCandidateRequestDto.UserName), nameof(AddOrUpdateCandidateRequestDto.UserName) },
                { new StringContent(updateCandidateRequestDto.Password), nameof(AddOrUpdateCandidateRequestDto.Password) },
                { new StringContent(updateCandidateRequestDto.Qualification ?? ""), nameof(AddOrUpdateCandidateRequestDto.Qualification) },
                { new StringContent(updateCandidateRequestDto.DateOfBirth?.ToString() ?? ""), nameof(AddOrUpdateCandidateRequestDto.DateOfBirth) },
                { new StringContent(updateCandidateRequestDto.Address ?? ""), nameof(AddOrUpdateCandidateRequestDto.Address) },
                { new StringContent(updateCandidateRequestDto.Mobile), nameof(AddOrUpdateCandidateRequestDto.Mobile) },
                { new StringContent(updateCandidateRequestDto.Email), nameof(AddOrUpdateCandidateRequestDto.Email) },
                { new StringContent(updateCandidateRequestDto.Gender.ToString()), nameof(AddOrUpdateCandidateRequestDto.Gender) },
                { new StringContent(updateCandidateRequestDto.RegistrationCenterCode ?? ""), nameof(AddOrUpdateCandidateRequestDto.RegistrationCenterCode) },
                { new StringContent(updateCandidateRequestDto.RegistrationDateTime.ToString("o") ?? ""), nameof(AddOrUpdateCandidateRequestDto.RegistrationDateTime) }
            };

            if (photoFile != null)
            {
                var photoStream = photoFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                formData.Add(new StreamContent(photoStream), nameof(AddOrUpdateCandidateRequestDto.PhotoFile), photoFile.Name);
            }

            if (signatureFile != null)
            {
                var signatureStream = signatureFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                formData.Add(new StreamContent(signatureStream), nameof(AddOrUpdateCandidateRequestDto.SignatureFile), signatureFile.Name);
            }

            var httpClient = _httpClientHelper._httpClient;

            var response = await httpClient.PutAsync("/api/Candidate/UpdateCandidate", formData);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
            }

            var error = await response.Content.ReadAsStringAsync();

            return new ApiResponse { StatusCode = response.StatusCode, CustomCodeStatus = CustomCodeStatus.Failure, Message = error };
        }

        public async Task<ApiResponse> AddMultipleCandidateAsync(CandidateAndLookUpsRequestDto candidateAndLookUpsRequestDto)
        {
            return await _httpClientHelper.PostMultipartAsJsonAsync(candidateAndLookUpsRequestDto, "api/Candidate/AddMultipleCandidates");
        }

        public Task<ApiResponse> DeleteCandidate(long candidateId)
        {
            return _httpClientHelper.PostAsync(candidateId, $"api/Candidate/DeleteCandidate");
        }

        public Task<ApiResponse> DownloadCandidateTemplate(GetOrganizationRootResponseDto selectedRoot)
        {
            return _httpClientHelper.PostAsync(selectedRoot, "api/Candidate/DownloadCandidatesUploadRelatedFiles");
        }

        public async Task<ApiResponse> AddMultipleCandidateAndSchedulePaperAsync(CandidateAndLookUpsAndSchedulePapersRequestDto candidateAndLookUpsRequestDto)
        {
            var respone = await _httpClientHelper.PostMultipartAsJsonAsync(candidateAndLookUpsRequestDto, "api/Candidate/AddMultipleCandidatesToSchedulePaper");

            return respone;
        }

        public async Task<ApiResponse> ValidateCandidatesDataAsync(CandidateDataCompositeRequestDto candidateDataCompositeRequestDto)
        {
            var respone = await _httpClientHelper.PostMultipartAsJsonAsync(candidateDataCompositeRequestDto, "api/Candidate/ValidateCandidates");

            return respone;
        }

        public async Task<ApiResponse> DumpImportCandidatesWithSchedulePaperAsync(DumpImportCandidatesWithSchedulePaperRequestDto dumpImportCandidatesWithSchedulePaperRequestDto)
        {
            return await _httpClientHelper.PostMultipartAsJsonAsync(dumpImportCandidatesWithSchedulePaperRequestDto, "api/Candidate/DumpImportCandidatesWithSchedulePaperAsync");
        }

        public async Task<ApiResponse> AllocateCandidateByLookUpsIds(AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto)
        {
            return await _httpClientHelper.PostAsync(allocateLookUpsSchedulePaperRequestDto, "api/Candidate/AllocateCandidateByLookUpsIds");
        }

        public async Task<ApiResponse> DownloadCandidatesWithExistingVenuesExcelFileAsync()
        {
            return await _httpClientHelper.GetAsync<CandidateTemplateDownloadDto>(
                "api/Candidate/DownloadCandidatesWithExistingVenuesExcelFile"
            );
        }

        //public async Task<List<GetOESGroupDto>> GetUserCandidateGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Candidate/GetUserCandidateGroupsAsync");
        //    var data = (List<GetOESGroupDto>)response.Data;
        //    return data ?? [];
        //}

        //public async Task<CandidateGroupDto> GetCandidateGroupsAsync(long candidateId)
        //{
        //    var response = await _httpClientHelper.GetAsync<CandidateGroupDto>($"api/Candidate/GetCandidateGroupsAsync?candidateId={candidateId}");
        //    return response.Data as CandidateGroupDto ?? new CandidateGroupDto();
        //}
    }
}