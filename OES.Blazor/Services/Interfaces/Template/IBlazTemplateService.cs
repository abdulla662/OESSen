using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Template
{
    public interface IBlazTemplateService
    {
        Task<TemplateAttributesDto> GetTemplateAttributesAsync(long templateTypeId);
        Task<ApiResponse> SaveTemplateAsync(AddTemplateRequestDto addTemplateRequestDto);
        Task<ApiResponse> DeleteTemplate(long id);
        Task<ApiResponse> UpdateTemplate(TemplateDataDto _dto);
        Task<CustomTableData<TemplateDataDto>> GetAllTemplates(PaginationSearchModel paginationSearchModel);
        Task<List<GetListedTemplateResponseDto>> GetAllListedTemplatesAsync();
        Task<List<TemplateTypeDto>> GetTemplateTypes();
        Task<TemplateDataDto> GetTemplateDataById(long id);
        Task<List<GetListedTemplateResponseDto>> GetTemplatesByTypeId(long templateTypeId);
    }
}