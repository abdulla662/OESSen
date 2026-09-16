using OES.Helper.General;
using OES.Helper.Dtos.QuestionLayout;

namespace OES.Interface.Interfaces
{
    public interface IQuestionLayoutService
    {
        Task<ApiResponse> GetAllLayoutsByQuestionTypeId(long questionTypeId);

        Task<ApiResponse> AssignLayoutToQuestionMetadataAsync(long questionMetadataId, long layoutId);

        Task<Dictionary<long, LayoutDto>> GetDefaultLayoutIdsByQuestionTypeIdsAsync(IEnumerable<long> questionTypeIds);
    }
}
