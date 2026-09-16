using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.QuestionLayout;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.QuestionLayout
{
    public class BlazQuestionLayoutService(IHttpClientHelper _httpClientHelper) : IBlazQuestionLayoutService
    {
        public async Task<List<LayoutDto>> GetAllLayoutsByQuestionTypeId(long questionTypeId)
        {
            var result = await _httpClientHelper.GetAsync<List<LayoutDto>>($"api/QuestionLayout/GetLayoutsByQuestionTypeId?questionTypeId={questionTypeId}");

            return result.Data is not null ? (List<LayoutDto>)result.Data : [];
        }

        public async Task<ApiResponse> AssignLayoutToQuestionMetadataAsync(long questionMetadataId, long layoutId)
        {
            var response = await _httpClientHelper.GetAsync<ApiResponse>($"api/QuestionLayout/AssignLayoutToQuestionMetadata?{nameof(questionMetadataId)}={questionMetadataId}&&{nameof(layoutId)}={layoutId}");

            return response;
        }
    }
}
