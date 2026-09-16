using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.GroupsTempleates
{
    public interface IBlazOesRoleTemplateService
    {
        Task<CustomTableData<CreateTemplateDto>> GetAllRolesAndTemplate(PaginationSearchModel pagination, bool IsTemplate, ResourceType? resourceType = null);
        Task<CreateTemplateDto> GetTemplateDetailsAsync(Guid id);
        Task<ApiResponse> DuplicateTemplateAsync(Guid TemplateId, string newName);
        Task<ApiResponse> DeleteTemplateAsync(Guid TemplateId);
        Task<ApiResponse> UpdateTemplateAsync(CreateTemplateDto CreateModel);
        Task<Dictionary<ResourceType, List<string>>> GetAllTemplateRolesAsync();
        Task<ApiResponse> CreateTemplateAsync(CreateTemplateDto CreateModel);
    }
}
