using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IQuestionValidatorService
    {
        ApiResponse ValidateAddedOrUpdatedQuestionMetadata(QuestionMetadataAdditionOrUpdateDto dto);

        Task<ApiResponse> ValidateNewLanguageDetails(QuestionDetailsDto dto);
    }
}
