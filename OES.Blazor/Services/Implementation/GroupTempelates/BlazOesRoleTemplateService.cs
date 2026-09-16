using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.GroupTempelates
{
    public class BlazOesRoleTemplateService : IBlazOesRoleTemplateService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<CreateTemplateDto> _blazTemplateTableData;

        public BlazOesRoleTemplateService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<CreateTemplateDto> blazTemplateTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazTemplateTableData = blazTemplateTableData;
        }

        public async Task<CustomTableData<CreateTemplateDto>> GetAllRolesAndTemplate(PaginationSearchModel pagination, bool IsTemplate, ResourceType? resourceType = null)
        {
            string Response = $"api/OesRoleTemplate/getAll?IsTemplate={IsTemplate}";

            if (resourceType.HasValue && resourceType.Value != ResourceType.All)
                Response += $"&ResourceType={resourceType.Value}";

            return await _blazTemplateTableData.GetCustomTableData(pagination, Response);
        }

        public async Task<CreateTemplateDto> GetTemplateDetailsAsync(Guid id)
        {
            var response = await _httpClientHelper.GetAsync<CreateTemplateDto>($"api/OesRoleTemplate/getById?id={id}");

            if (response?.Data == null)
                return new CreateTemplateDto();

            return response.Data as CreateTemplateDto ?? new CreateTemplateDto();
        }

        public async Task<ApiResponse> DuplicateTemplateAsync(Guid id, string newName)
        {
            var response = await _httpClientHelper.PostAsync(null, $"api/OesRoleTemplate/duplicate?id={id}&newName={newName}");
            return response;
        }

        public async Task<ApiResponse> DeleteTemplateAsync(Guid id)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/OesRoleTemplate/delete?id={id}");
            return response;
        }

        public async Task<ApiResponse> UpdateTemplateAsync(CreateTemplateDto model)
        {
            var response = await _httpClientHelper.PutAsync(model, "api/OesRoleTemplate/update");
            return response;
        }

        public async Task<Dictionary<ResourceType, List<string>>> GetAllTemplateRolesAsync()
        {
            var response = await _httpClientHelper.GetAsync<Dictionary<ResourceType, List<string>>>("api/OesRoleTemplate/GetAllTemplateRoles");

            return (Dictionary<ResourceType, List<string>>)(response.Data ?? new Dictionary<ResourceType, List<string>>());
        }

        public async Task<ApiResponse> CreateTemplateAsync(CreateTemplateDto model)
        {
            return await _httpClientHelper.PostAsync(model, "api/OesRoleTemplate/create");
        }
    }
}
