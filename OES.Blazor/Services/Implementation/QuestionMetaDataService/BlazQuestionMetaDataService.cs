using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.General;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.QuestionMetaDataService
{
    public class BlazQuestionMetaDataService(IHttpClientHelper _httpClientHelper) : IBlazQuestionMetaData
    {
        public async Task<ApiResponse> GetQuestionMetaDataForQCView(long Id)
        {
            var response = await _httpClientHelper.GetAsync<GetQuestionMetaDataForQCViewDto>($"api/QuestionMetadata/GetQuestionMetaDataForQCView?questionId={Id}");

            return response;
        }

        public async Task<QuestionMetadataCheckResultDto> CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(long questionMetadataId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionMetadataCheckResultDto>($"api/QuestionMetadata/CheckIfQuestionMetadataHasAnyQuestionDetails?{nameof(questionMetadataId)}={questionMetadataId}");

            return (QuestionMetadataCheckResultDto)response.Data;
        }

        public async Task<ApiResponse> IsTheDocumentUsedInAnyQuestionAsync(Guid documentId)
        {
            return await _httpClientHelper.GetAsync<object>($"api/QuestionMetadata/IsTheDocumentUsedInAnyQuestion?{nameof(documentId)}={documentId}");
        }

        public async Task<ApiResponse> CopyOldQuestionDeeplyAsync(long questionMetadataId)
        {
            var response = await _httpClientHelper.GetAsync<object>($"api/QuestionMetadata/CopyOldQuestionDeeplyAsync?{nameof(questionMetadataId)}={questionMetadataId}");

            return response;
        }

        public async Task<ApiResponse> GetQuestionVersionsAsync(long questionMetadataId)
        {
            return await _httpClientHelper.GetAsync<List<QuestionDetailsVersionDto>>($"api/QuestionMetadata/GetQuestionVersions?{nameof(questionMetadataId)}={questionMetadataId}");
        }

        public async Task<ApiResponse> UpdateQuestionDeltaFromExcel(UpdateQuestionDeltaConfirmDto updateQuestionDeltaConfirmDto)
        {
            return await _httpClientHelper.PostAsync(updateQuestionDeltaConfirmDto, "api/QuestionMetadata/UpdateQuestionDeltaFromExcel");
        }

        public async Task<ApiResponse> ValidateQuestionDeltaFromExcelAsync(UpdateQuestionDeltaRequestDto updateQuestionDeltaRequestDto)
        {
            using var formData = new MultipartFormDataContent();

            using var streamContent = new StreamContent(updateQuestionDeltaRequestDto.File.OpenReadStream());

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(updateQuestionDeltaRequestDto.File.ContentType);

            formData.Add(streamContent, MiscConstants.File, updateQuestionDeltaRequestDto.File.FileName);

            var response = await _httpClientHelper._httpClient.PostAsync("api/QuestionMetadata/ValidateQuestionDeltaFromExcel", formData);

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
