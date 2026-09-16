using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QuestionLayout
{
    public interface IBlazQuestionLayoutService
    {
        Task<List<LayoutDto>> GetAllLayoutsByQuestionTypeId(long questionTypeId);

        Task<ApiResponse> AssignLayoutToQuestionMetadataAsync(long questionMetadataId, long layoutId);
    }
}
