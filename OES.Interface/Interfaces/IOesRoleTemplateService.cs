using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IOesRoleTemplateService
    {
        Task<ApiResponse> GetTemplatesWithResourcesAsync(bool isTemplate, PaginationSearchModel pagination, ResourceType? resourceType = null);
        Task<ApiResponse> GetTemplateDetailsAsync(Guid id);
        Task<ApiResponse> DuplicateTemplateAsync(Guid id, string newName);
        Task<ApiResponse> DeleteTemplateAsync(Guid id);
        Task<ApiResponse> UpdateTemplateAsync(CreateTemplateDto model);
        Task<ApiResponse> GetRolesByResourceAsync();
        Task<ApiResponse> CreateCustomTemplateAsync(CreateTemplateDto model);
    }
}
