using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.EquationTemplate
{
    public interface IBlazEquationTemplateService
    {
        Task<CustomTableData<EquationTemplatePaginationDto>> GetAllEquationTemplatesAsync(PaginationSearchModel pagination);

        Task<ApiResponse> AddEquationTemplateAsync(AddOrUpdateEquationTemplateDto request);

        Task<ApiResponse> GetEquationTemplateById(long id);

        Task<ApiResponse> UpdateEquationTemplateAsync(AddOrUpdateEquationTemplateDto request);

        Task<ApiResponse> DeleteEquationTemplateAsync(long id);

        Task<ApiResponse> GetCandidateQuestionsWithEquation(long formId);

        Task<ApiResponse> ExportCandidatesData(ExportCandidatesRequestDto request);

        Task<ApiResponse> GenerateResultsBatchAsync(ExportCandidateRequestByDateDto request);

        Task<ApiResponse> ExportCandidatesDataBatchAsync(ExportCandidatesBatchRequestDto request);

        Task<List<ItemBanksFromItemBankPointResponseDto>> GetAllItemBanksFromQuestionBlocksAsync(long paperId);

        Task<List<CandidateResultDto>> GetCandidateResultsAsync(ExportCandidatesRequestDto request);

        //Task<List<GetOESGroupDto>> GetUserEquationGroupsAsync();

        //Task<EquationGroupDto> GetEquationGroupsAsync(long EquationId);
    }
}