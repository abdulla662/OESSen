using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;
namespace OES.Interface.Interfaces
{
    public interface IQuestionUploadTemplateService
    {
        Task<ApiResponse> GetAllPaginatedTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetByIdAsync(long templateId);

        Task<ApiResponse> AddTemplateAsync(QuestionUploadTemplateDto questionUploadTemplateDto);

        Task<ApiResponse> DeleteTemplateAsync(long templateId);
    }
}
