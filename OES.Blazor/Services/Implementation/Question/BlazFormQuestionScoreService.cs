using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Question
{
    public class BlazFormQuestionScoreService : IBlazFormQuestionScoreService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazFormQuestionScoreService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ApiResponse> GetQuestionForPaperAsync(long paperId)
        {
            return await _httpClientHelper.PostAsync(paperId, "api/SyncQuestion/GetQuestionForPaperAsync");
        }
    }
}
