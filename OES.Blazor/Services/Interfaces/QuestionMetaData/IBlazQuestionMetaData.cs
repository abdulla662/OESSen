using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QuestionMetaData
{
    public interface IBlazQuestionMetaData
    {
        Task<ApiResponse> GetQuestionMetaDataForQCView(long Id);

        Task<QuestionMetadataCheckResultDto> CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(long questionMetadataId);

        Task<ApiResponse> IsTheDocumentUsedInAnyQuestionAsync(Guid documentId);

        Task<ApiResponse> CopyOldQuestionDeeplyAsync(long questionMetadataId);

        Task<ApiResponse> GetQuestionVersionsAsync(long questionMetadataId);

        Task<ApiResponse> UpdateQuestionDeltaFromExcel(UpdateQuestionDeltaConfirmDto updateQuestionDeltaConfirmDto);

        Task<ApiResponse> ValidateQuestionDeltaFromExcelAsync(UpdateQuestionDeltaRequestDto updateQuestionDeltaRequestDto);
    }
}
