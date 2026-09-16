using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Question
{
    public class BlazQuestionDistributionValidationService : IBlazQuestionDistributionValidationService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazQuestionDistributionValidationService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> ValidateQuestionTypeAndDifficultyDistributionAsync(ValidateQuestionRequestDto request)
        {
            var result = await _httpClientHelper.PostAsync(request, "api/Question/ValidateQuestionTypeAndDifficultyDistributionAsync");

            return result;
        }
    }
}
