using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class TemplateController : OESBaseController
    {
        private readonly ITemplateService TemplateService;


        public TemplateController(ITemplateService _templateService)
        {
            TemplateService = _templateService;
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetTemplateDataById")]
        public async Task<IApiResponse> GetTemplateDataById(long templateId)
        {
            return await TemplateService.GetTemplateDataById(templateId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllTemplates")]
        public async Task<IApiResponse> GetAllTemplates(PaginationSearchModel paginationSearchModel)
        {
            return await TemplateService.GetAllTemplates(paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAllListedTemplates")]
        public async Task<IApiResponse> GetAllListedTemplatesAsync()
        {
            return await TemplateService.GetAllListedTemplatesAsync();
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetTemplateTypes")]
        public async Task<IApiResponse> GetTemplateTypes()
        {
            return await TemplateService.GetTemplateTypes();
        }


        [HttpGet("GetTemplateAttributesById")]
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        public async Task<IApiResponse> GetTemplateAttributesById(long templateTypeId)
        {
            return await TemplateService.GetTemplateAttributesById(templateTypeId);
        }


        [HttpGet("GetTemplatesByTypeId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetTemplatesByTypeId(long templateTypeId)
        {
            return await TemplateService.GetTemplatesByTypeId(templateTypeId);
        }


        [HttpDelete("DeleteTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteTemplate(long templateId)
        {
            return await TemplateService.DeleteTemplate(templateId);
        }


        [HttpPost("AddTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddTemplate(AddTemplateRequestDto addTemplateRequestDto)
        {
            return await TemplateService.AddTemplate(addTemplateRequestDto);
        }


        [HttpPut("UpdateTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateTemplate(UpdateTemplateDto _dto)
        {
            return await TemplateService.UpdateTemplate(_dto);
        }
    }
}
