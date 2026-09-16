using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Blazor.Services.Interfaces.QuestionType
{
    public interface IBLazQuestionType
    {
        Task<CustomTableData<QuestionTypeDto>> PaginationQuestionType(PaginationSearchModel pagination);
        Task<IApiResponse> CreateQuestionType(QuestionTypeDto questionType);
        Task<IApiResponse> UpdateQuestionType(QuestionTypeDto questionType);
        Task<IApiResponse> DeleteQuestionType(long id);
        Task<List<DifficultyLevelDto>> GetAllDifficultyLevels(long profileId);
        Task<ApiResponse> GetQuestionType(long Id);
        Task<List<QuestionTypeDto>> GetAllQuestionType();
    }
}
