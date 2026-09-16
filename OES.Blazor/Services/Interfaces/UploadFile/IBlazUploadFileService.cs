using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.UploadFile
{
    public interface IBlazUploadFileService
    {
        Task<ApiResponse> ValidateQuestionsFile(IFormFile formFilem, List<string> questionTypeNames);
        Task<ApiResponse> AddQuestionsAndMetaData(string validQuestionsJson, QuestionMetadataAdditionWithUploadFile questionMetadataAdditionWithUpload);
    }
}
