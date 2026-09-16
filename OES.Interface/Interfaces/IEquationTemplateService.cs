using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IEquationTemplateService
    {
        Task<ApiResponse> AddEquationTemplateAsync(AddOrUpdateEquationTemplateDto addOrUpdateEquationTemplateDto, CancellationToken cancellationToken = default);

        Task<ApiResponse> GetAllEquationTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetEquationTemplateById(long id);

        //Task<ApiResponse> GetUserEquationGroupsAsync();

        //Task<ApiResponse> GetEquationGroupsAsync(long blockId);

        Task<ApiResponse> UpdateEquationTemplateAsync(AddOrUpdateEquationTemplateDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteEquationTemplateAsync(long id, CancellationToken cancellationToken = default);

        Task<ApiResponse> GetCandidateQuestionsWithEquation(long formId);

        Task<ApiResponse> ExportCandidatesData(ExportCandidatesRequestDto request);

        Task<ApiResponse> GetAllItemBanksFromQuestionBlocksAsync(long paperId);

        Task<ApiResponse> GetCandidateResultsAsync(ExportCandidatesRequestDto request);

        /// <summary>
        /// Optimized batch result generation that fetches all data in single queries
        /// and processes in memory. Used for Simple Mode result generation.
        /// </summary>
        Task<ApiResponse> GenerateResultsBatchAsync(
            ExportCandidateRequestByDateDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimized batch export for Advanced Mode. Accepts multiple form/venue combinations
        /// in a single request, fetches data once, and processes each combination using
        /// the same business logic as ExportCandidatesData.
        /// </summary>
        Task<ApiResponse> ExportCandidatesDataBatchAsync(
            ExportCandidatesBatchRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
