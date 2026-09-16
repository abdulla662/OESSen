using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface ITemplateService
    {
        Task<IApiResponse> GetTemplateDataById(long id);
        Task<IApiResponse> GetTemplateAttributesById(long templateTypeId);
        Task<ApiResponse> GetTemplatesByTypeId(long templateTypeId);
        Task<IApiResponse> DeleteTemplate(long id);
        Task<IApiResponse> AddTemplate(AddTemplateRequestDto addTemplateRequestDto);
        Task<IApiResponse> UpdateTemplate(UpdateTemplateDto _dto);
        Task<IApiResponse> GetAllTemplates(PaginationSearchModel paginationSearchModel);
        Task<IApiResponse> GetAllListedTemplatesAsync();
        Task<IApiResponse> GetTemplateTypes();
    }
}
