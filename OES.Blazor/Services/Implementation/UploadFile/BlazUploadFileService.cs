using Microsoft.AspNetCore.Http;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.UploadFile;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;
using SharedHelper.General;
using System.Text;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.UploadFile
{
    public class BlazUploadFileService(IHttpClientHelper _httpClientHelper) : IBlazUploadFileService
    {
        public async Task<ApiResponse> ValidateQuestionsFile(IFormFile formFile, List<string> questionTypeNames)
        {
            if (formFile == null)
            {
                return new ApiResponse
                {
                    Message = "No file selected."
                };
            }

            using var formData = new MultipartFormDataContent();

            using var streamContent = new StreamContent(formFile.OpenReadStream());

            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(formFile.ContentType);

            formData.Add(streamContent, "formFile", formFile.FileName);

            var questionTypeNamesJson = JsonSerializer.Serialize(questionTypeNames);

            var typeNames = new StringContent(questionTypeNamesJson, Encoding.UTF8, "application/json");

            formData.Add(typeNames, "questionTypeNames");

            var response = await _httpClientHelper._httpClient.PostAsync($"{CentralizedUrlHelper.OesApiBaseUrl}api/Upload/UploadFile", formData);

            return JsonSerializer.Deserialize<ApiResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse> AddQuestionsAndMetaData(string validQuestionsJson, QuestionMetadataAdditionWithUploadFile questionMetadataAdditionWithUpload)
        {
            if (string.IsNullOrEmpty(validQuestionsJson))
            {
                return new ApiResponse
                {
                    Message = "No valid questions to add."
                };
            }

            if (questionMetadataAdditionWithUpload is null)
            {
                return new ApiResponse
                {
                    Message = "No valid Meta data to add."
                };
            }

            using var formData = new MultipartFormDataContent();

            var questionsContent = new StringContent(validQuestionsJson, Encoding.UTF8, "application/json");
            formData.Add(questionsContent, "uploadQuestions");

            questionMetadataAdditionWithUpload.QuestionStatus = QuestionStatus.LayoutSelectedAndPending;

            var metadataJson = JsonSerializer.Serialize(new
            {
                questionMetadataAdditionWithUpload.Code,
                questionMetadataAdditionWithUpload.DifficultyProfileId,
                questionMetadataAdditionWithUpload.DifficultyLevelId,
                questionMetadataAdditionWithUpload.Delta,
                questionMetadataAdditionWithUpload.IsRoot,
                questionMetadataAdditionWithUpload.MaximumAnswerTime,
                questionMetadataAdditionWithUpload.Author,
                questionMetadataAdditionWithUpload.QuestionTypeId,
                questionMetadataAdditionWithUpload.QuestionSubjectId,
                questionMetadataAdditionWithUpload.QuestionCategoryId,
                questionMetadataAdditionWithUpload.IloId,
                questionMetadataAdditionWithUpload.ItemBankId,
                questionMetadataAdditionWithUpload.LanguageId,
                questionMetadataAdditionWithUpload.QuestionsExhaustionCount,
                questionMetadataAdditionWithUpload.QuestionStatus,
                questionMetadataAdditionWithUpload.FileManagerEditorPanelEnabled,
                questionMetadataAdditionWithUpload.ScientificEditorPanelEnabled,
                questionMetadataAdditionWithUpload.UploadMode
            });

            var metadataContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");
            formData.Add(metadataContent, "metadata");

            var response = await _httpClientHelper._httpClient.PostAsync($"{CentralizedUrlHelper.OesApiBaseUrl}api/Upload/AddFile", formData);

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
