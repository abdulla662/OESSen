using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IQuestionMetadataService
    {
        Task<IApiResponse> GetPaginatedSubQuestions(long parentId, PaginationSearchModel pagination);
        Task<IApiResponse> GetQuestionMetaDataForQCView(long questionId);
        Task<IApiResponse> CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(long questionMetadataId);
        Task<IApiResponse> CopyOldQuestionDeeplyAsync(long questionMetadataId);
        Task<ApiResponse> IsTheDocumentUsedInAnyQuestionAsync(Guid documentId);
        Task<IApiResponse> GetQuestionVersionsAsync(long questionMetadataId);
        Task<IApiResponse> ValidateQuestionDeltaFromExcelAsync(UpdateQuestionDeltaRequestDto updateQuestionDeltaRequestDto);
        Task<IApiResponse> UpdateQuestionDeltaAsync(UpdateQuestionDeltaConfirmDto updateQuestionDeltaConfirmDto);
    }
}
