using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.SectionDistributionDto.Common;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Question
{
    public interface IBlazQuestionDistributionValidationService
    {
        Task<ApiResponse> ValidateQuestionTypeAndDifficultyDistributionAsync(ValidateQuestionRequestDto request);
    }
}
