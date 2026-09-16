using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class OesRoleTemplateController : OESBaseController
    {
        private readonly IOesRoleTemplateService _templateService;

        public OesRoleTemplateController(IOesRoleTemplateService templateService)
        {
            _templateService = templateService;
        }

        /// <summary>
        /// Retrieves all templates (predefined or custom) with optional filtering by ResourceType
        /// and supports pagination for large datasets.
        /// This endpoint is used to list templates/groups in the management screens.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpPost("getAll")]
        public async Task<ApiResponse> GetAll(
            [FromQuery] bool IsTemplate,
            [FromQuery] ResourceType? ResourceType,
            [FromBody] PaginationSearchModel pagination
        )
        {
            return await _templateService.GetTemplatesWithResourcesAsync(IsTemplate, pagination, ResourceType);
        }

        /// <summary>
        /// Retrieves full details of a specific template by its ID, including:
        /// - Assigned resources
        /// - Roles linked to each resource
        /// - Template metadata (name, description, type)
        /// Used when opening a template for viewing or editing.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpGet("getById")]
        public async Task<ApiResponse> GetById(Guid id)
        {
            return await _templateService.GetTemplateDetailsAsync(id);
        }

        /// <summary>
        /// Soft-deletes a template by marking it as deleted.
        /// This operation preserves system integrity by not removing related role/resource mappings.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpDelete("delete")]
        public async Task<ApiResponse> Delete(Guid id)
        {
            return await _templateService.DeleteTemplateAsync(id);
        }

        /// <summary>
        /// Updates an existing template with new details, resources, and assigned roles.
        /// Ensures that each selected resource has at least one role before saving.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpPut("update")]
        public async Task<IApiResponse> UpdateTemplate([FromBody] CreateTemplateDto model)
        {
            return await _templateService.UpdateTemplateAsync(model);
        }

        /// <summary>
        /// Retrieves all available system roles grouped by ResourceType (e.g., Question, Paper, ItemBank).
        /// Used when building or editing templates that require role assignment.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpGet("getAllTemplateRoles")]
        public async Task<ApiResponse> GetAllTemplateRoles()
        {
            return await _templateService.GetRolesByResourceAsync();
        }

        /// <summary>
        /// Creates a new custom template (non-predefined) with its selected resources and assigned roles.
        /// Performs validation to ensure uniqueness of template name and valid role assignments.
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpPost("create")]
        public async Task<ApiResponse> Create([FromBody] CreateTemplateDto model)
        {
            return await _templateService.CreateCustomTemplateAsync(model);
        }

        /// <summary>
        /// Creates a full duplicate of an existing template using a new name.
        /// The duplication includes:
        /// - All resource associations
        /// - All assigned roles
        /// - All linked users
        /// </summary>
        [OESFilter(Authorize = true)]
        [HttpPost("duplicate")]
        public async Task<ApiResponse> Duplicate(Guid id, string newName)
        {
            return await _templateService.DuplicateTemplateAsync(id, newName);
        }
    }
}
