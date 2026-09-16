using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QuestionUploadTemplate
{
    public interface IBlazQuestionUploadTemplateService
    {
        Task<CustomTableData<QuestionUploadTemplateDto>> GetAllPaginatedTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<QuestionUploadTemplateDto> GetTemplateByIdAsync(long templateId);

        Task<ApiResponse> AddTemplateAsync(QuestionUploadTemplateDto questionUploadTemplateDto);

        Task<ApiResponse> DeleteTemplateAsync(long templateId);
    }
}
