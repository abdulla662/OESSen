using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Template
{
    public class BlazTemplateService(IHttpClientHelper _httpClient, IBlazGetCustomTableData<TemplateDataDto> blazGetCustomTable) : IBlazTemplateService
    {
        public async Task<ApiResponse> DeleteTemplate(long templateId)
        {
            return await _httpClient.DeleteAsync($"api/Template/DeleteTemplate?templateId={templateId}");
        }


        public async Task<CustomTableData<TemplateDataDto>> GetAllTemplates(PaginationSearchModel paginationSearchModel)
        {
            return await blazGetCustomTable.GetCustomTableData(paginationSearchModel, "api/Template/GetAllTemplates");
        }


        public async Task<List<GetListedTemplateResponseDto>> GetAllListedTemplatesAsync()
        {
            var response = await _httpClient.GetAsync<List<GetListedTemplateResponseDto>>($"api/Template/GetAllListedTemplates");

            return (List<GetListedTemplateResponseDto>)response.Data ?? [];
        }


        public async Task<TemplateAttributesDto> GetTemplateAttributesAsync(long templateTypeId)
        {
            var response = await _httpClient.GetAsync<TemplateAttributesDto>($"api/Template/GetTemplateAttributesById?templateTypeId={templateTypeId}");

            return (TemplateAttributesDto)response.Data;
        }


        public async Task<TemplateDataDto> GetTemplateDataById(long templateId)
        {
            var response = await _httpClient.GetAsync<TemplateDataDto>($"api/Template/GetTemplateDataById?templateId={templateId}");

            return (TemplateDataDto)response.Data;
        }


        public async Task<List<GetListedTemplateResponseDto>> GetTemplatesByTypeId(long templateTypeId)
        {
            var response = await _httpClient.GetAsync<List<GetListedTemplateResponseDto>>($"api/Template/GetTemplatesByTypeId?templateTypeId={templateTypeId}");

            return (List<GetListedTemplateResponseDto>)response.Data ?? [];
        }


        public async Task<List<TemplateTypeDto>> GetTemplateTypes()
        {
            var response = await _httpClient.GetAsync<List<TemplateTypeDto>>("api/Template/GetTemplateTypes");

            return (List<TemplateTypeDto>)response.Data;
        }


        public async Task<ApiResponse> SaveTemplateAsync(AddTemplateRequestDto addTemplateRequestDto)
        {
            return await _httpClient.PostAsync(addTemplateRequestDto, "api/Template/AddTemplate");
        }


        public Task<ApiResponse> UpdateTemplate(TemplateDataDto updateTemplateDto)
        {
            return _httpClient.PutAsync(updateTemplateDto, "api/Template/UpdateTemplate");
        }
    }
}