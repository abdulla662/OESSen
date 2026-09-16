using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IQuestionDistributionValidationService
    {
        Task<ApiResponse> ValidateQuestionTypeAndDifficultyDistributionAsync(AddOrUpdateAutoSectionsDistributionsRequestDto request);
    }
}
