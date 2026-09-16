using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using System.Net;
using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Services.Services
{
    public class OesRoleTemplateService : IOesRoleTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiResponse _apiResponse;

        public OesRoleTemplateService(IUnitOfWork unitOfWork, IApiResponse apiResponse)
        {
            _unitOfWork = unitOfWork;
            _apiResponse = apiResponse;
        }

        public async Task<ApiResponse> GetTemplatesWithResourcesAsync(
            bool isTemplate,
            PaginationSearchModel pagination,
            ResourceType? resourceType = null
        )
        {
            pagination ??= new PaginationSearchModel();

            var query = _unitOfWork.Repository<OESGroup, Guid>().GetAll(t => !t.IsDeleted && t.IsTemplate == isTemplate && !t.AutoCreatedForUser);

            if (resourceType != null && resourceType.Value != ResourceType.All)
            {
                var typeValue = resourceType.Value;

                query = query.Where(t =>
                    t.GroupResources.Any() &&
                    t.GroupResources.All(r => r.ResourceType == typeValue));
            }

            if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
            {
                var key = pagination.SearchKey.Trim().ToLower();

                if (pagination.SearchInName && pagination.SearchInDescription)
                {
                    query = query.Where(t => t.Name.ToLower().Contains(key) || (t.Description != null && t.Description.ToLower().Contains(key)));
                }
                else if (pagination.SearchInDescription)
                {
                    query = query.Where(t => t.Description != null && t.Description.ToLower().Contains(key));
                }
                else
                {
                    query = query.Where(t => t.Name.ToLower().Contains(key));
                }
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(t => t.CreationDate >= pagination.FromDate.Value);
            }

            if (pagination.ToDate.HasValue)
            {
                query = query.Where(t => t.CreationDate <= pagination.ToDate.Value);
            }

            var total = await query.CountAsync();

            query = pagination.OrderBy == SearchInKey.ASC
                ? query.OrderBy(x => x.CreationDate)
                : query.OrderByDescending(x => x.CreationDate);

            if (!pagination.PaginationOff)
            {
                query = query
                    .Skip(pagination.PageIndex * pagination.PageSize)
                    .Take(pagination.PageSize);
            }

            var templateEntities = await query
                .AsNoTracking()
                .Include(t => t.GroupResources)
                    .ThenInclude(r => r.ResourceRoles)
                        .ThenInclude(rr => rr.Role)
                .Include(t => t.GroupResources)
                    .ThenInclude(r => r.Resource)
                .ToListAsync();

            if (templateEntities == null || templateEntities.Count == 0)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    string.Empty,
                    new CustomTableData<CreateTemplateDto>([], 0));
            }

            var templateDtos = templateEntities.ConvertAll(t => new CreateTemplateDto
            {
                Id = t.Id,
                GroupId = t.Id,
                Name = t.Name,
                Description = t.Description,
                IsTemplate = t.IsTemplate,
                IsPredefined = t.IsPredefined,
                ResourcesWithRoles = [.. t.GroupResources
                    .Where(r =>
                        resourceType == null ||
                        resourceType == ResourceType.All ||
                        r.ResourceType == resourceType.Value)
                    .Select(r => new ResourceWithRolesDto
                    {
                        ResourceType = r.ResourceType,
                        Roles = r.ResourceRoles.Select(rr => rr.Role.Name).ToList()
                    })]
            });

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Empty,
                new CustomTableData<CreateTemplateDto>(templateDtos, total)
            );
        }

        public async Task<ApiResponse> GetTemplateDetailsAsync(Guid id)
        {
            var groupRepository = _unitOfWork.Repository<OESGroup, Guid>();

            var group = await groupRepository.GetAll()
                .Include(g => g.GroupResources)
                    .ThenInclude(gr => gr.Resource)
                .Include(g => g.GroupResources)
                    .ThenInclude(gr => gr.ResourceRoles)
                        .ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound
                );
            }

            var dto = new CreateTemplateDto
            {
                Id = group.Id,
                Name = group.Name,
                GroupId = group.Id,
                Description = group.Description,
                IsPredefined = group.IsPredefined,
                ResourcesWithRoles = [.. group.GroupResources.Select(gr => new ResourceWithRolesDto
                {
                    ResourceType = Enum.GetValues(typeof(ResourceType))
                        .Cast<ResourceType>()
                        .FirstOrDefault(t =>
                            string.Equals(t.ToString(), gr.Resource.Name, StringComparison.OrdinalIgnoreCase)),
                    Roles = [.. gr.ResourceRoles
                        .Where(rr => rr.Role != null && !rr.Role.IsDeleted)
                        .Select(rr => rr.Role.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)]
                })]
            };

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateDetailsLoaded,
                dto
            );
        }

        public async Task<ApiResponse> CreateCustomTemplateAsync(CreateTemplateDto model)
        {
            if (string.IsNullOrWhiteSpace(model?.Name))
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationNameRequired);
            }

            if (model.ResourcesWithRoles == null || !model.ResourcesWithRoles.Any())
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ResourcesRequireRoles);
            }

            foreach (var res in model.ResourcesWithRoles)
            {
                if (res.Roles == null || !res.Roles.Any())
                {
                    return _apiResponse.GetApiResponse(
                        CustomCodeStatus.ValidationError,
                        HttpStatusCode.BadRequest,
                        string.Format(Resource.ValidationRolesRequiredForResource, res.ResourceType)
                    );
                }
            }

            var groupRepository = _unitOfWork.Repository<OESGroup, Guid>();

            bool nameExists = await groupRepository.IsExistAsync(x =>
                !x.IsDeleted &&
                x.IsTemplate == model.IsTemplate &&
                x.Name.Trim().ToLower() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    model.IsTemplate
                        ? Resource.TemplateNameAlreadyExists
                        : Resource.GroupNameAlreadyExists);
            }

            var group = new OESGroup
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                IsTemplate = model.IsTemplate,
                IsPredefined = false,
                CreationUser = DefaultSystemUser.Name,
            };

            await groupRepository.AddAsync(group);
            await _unitOfWork.Complete();

            await AddResourcesWithRolesAsync(group, model.ResourcesWithRoles);
            await AddGroupRolesAsync(group.Id, model.ResourcesWithRoles);

            var successMessage = model.IsTemplate
                  ? string.Format(Resource.TemplateCreatedSuccessfully, model.Name)
                  : string.Format(Resource.GroupCreatedSuccessfully, model.Name);

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.Created,
                successMessage,
                group.Id);
        }

        public async Task<ApiResponse> UpdateTemplateAsync(CreateTemplateDto model)
        {
            foreach (var resourceAssignment in model.ResourcesWithRoles)
            {
                if (resourceAssignment.Roles == null || !resourceAssignment.Roles.Any())
                {
                    return _apiResponse.GetApiResponse(
                        CustomCodeStatus.ValidationError,
                        HttpStatusCode.BadRequest,
                        string.Format(Resource.ValidationRolesRequiredForResource, resourceAssignment.ResourceType)
                    );
                }
            }

            var groupRepository = _unitOfWork.Repository<OESGroup, Guid>();

            Guid targetId = model.GroupId != Guid.Empty ? model.GroupId : model.Id;
            if (targetId == Guid.Empty)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationTemplateIdMissing
                );
            }

            var group = await groupRepository.GetAll()
                .Include(g => g.GroupResources)
                    .ThenInclude(r => r.ResourceRoles)
                .FirstOrDefaultAsync(g => g.Id == targetId && !g.IsDeleted && !g.IsTemplate);

            if (group == null)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound
                );
            }

            bool nameExists = await groupRepository.IsExistAsync(x =>
                x.Id != group.Id &&
                !x.IsDeleted &&
                !x.IsTemplate &&
                x.Name.Trim().ToLower() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.TemplateNameAlreadyUsed);
            }

            group.Name = model.Name?.Trim();
            group.Description = model.Description?.Trim();

            var groupResourceRepository = _unitOfWork.Repository<OESGroupResource, long>();

            if (group.GroupResources?.Any() == true)
            {
                groupResourceRepository.DeleteRange([.. group.GroupResources]);
                await _unitOfWork.Complete();
            }

            var groupRoleRepo = _unitOfWork.Repository<OESGroupRole, long>();
            var oldGroupRoles = await groupRoleRepo.GetAllAsync(x => x.OESGroupId == group.Id);

            if (oldGroupRoles.Any())
            {
                groupRoleRepo.DeleteRange(oldGroupRoles);
                await _unitOfWork.Complete();
            }

            await AddResourcesWithRolesAsync(group, model.ResourcesWithRoles);
            await AddGroupRolesAsync(group.Id, model.ResourcesWithRoles);

            var successMessage = model.IsTemplate
                ? string.Format(Resource.TemplateUpdatedSuccessfully, model.Name)
                : string.Format(Resource.GroupUpdatedSuccessfully, model.Name);

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                successMessage,
                group.Id
            );
        }

        public async Task<ApiResponse> DuplicateTemplateAsync(Guid id, string newName)
        {
            var groupRepository = _unitOfWork.Repository<OESGroup, Guid>();

            if (string.IsNullOrWhiteSpace(newName))
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationNameRequired);
            }

            var existingTemplate = await groupRepository.GetAll()
               .Include(g => g.GroupResources)
                   .ThenInclude(r => r.ResourceRoles)
                       .ThenInclude(rr => rr.Role)
               .Include(g => g.GroupResources)
                   .ThenInclude(r => r.Resource)
               .Include(g => g.AppUserProfileGroups)
               .Include(g => g.Roles)
               .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted && !g.IsTemplate);

            if (existingTemplate == null)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound);
            }

            bool nameExists = await groupRepository.IsExistAsync(x =>
                !x.IsDeleted &&
                !x.IsTemplate &&
                x.Name.Trim().ToLower() == newName.Trim().ToLower());

            if (nameExists)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.TemplateNameAlreadyExists);
            }

            var newGroup = new OESGroup
            {
                Name = newName.Trim(),
                Description = existingTemplate.Description,
                IsTemplate = false,
                CreationUser = DefaultSystemUser.Name,
            };

            await groupRepository.AddAsync(newGroup);
            await _unitOfWork.Complete();

            var newResourcesList = existingTemplate.GroupResources
                .Select(gr => new ResourceWithRolesDto
                {
                    ResourceType = gr.ResourceType,
                    Roles = [.. gr.ResourceRoles
                        .Where(rr => rr.Role != null && !rr.Role.IsDeleted)
                        .Select(rr => rr.Role.Name)
                        .Distinct()]
                }).ToList();

            await AddResourcesWithRolesAsync(newGroup, newResourcesList);
            await AddGroupRolesAsync(newGroup.Id, newResourcesList);

            var usergroupRepository = _unitOfWork.Repository<AppUserProfileGroup, long>();
            if (existingTemplate.AppUserProfileGroups?.Any() == true)
            {
                var newUserroleAssignments = existingTemplate.AppUserProfileGroups.Select(u => new AppUserProfileGroup
                {
                    OESGroupId = newGroup.Id,
                    AppUserProfileId = u.AppUserProfileId,
                    CreationUser = DefaultSystemUser.Name
                }).ToList();

                usergroupRepository.AddRangAsync(newUserroleAssignments);
                await _unitOfWork.Complete();
            }

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.Created,
                Resource.TemplateDuplicatedSuccessfully,
                newGroup.Id
            );
        }

        public async Task<ApiResponse> DeleteTemplateAsync(Guid id)
        {
            var entity = await _unitOfWork
                .Repository<OESGroup, Guid>()
                .GetObjAsync(x => x.Id == id && x.IsTemplate);

            if (entity == null)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound);
            }

            entity.IsDeleted = true;
            await _unitOfWork.Complete();

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateDeletedSuccessfully);
        }

        public async Task<ApiResponse> GetRolesByResourceAsync()
        {
            var allRoles = await _unitOfWork.Repository<OESRole, long>()
                .GetAll()
                .Where(r => !r.IsDeleted)
                .Select(r => r.Name)
                .ToListAsync();

            if (allRoles == null || allRoles.Count == 0)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.RolesNotFound);
            }

            var groupedRoles = Enum.GetValues<ResourceType>()
                .ToDictionary(
                    res => res,
                    res => allRoles
                        .Where(role =>
                            role.StartsWith(res.GetRolePrefix(), StringComparison.OrdinalIgnoreCase))
                        .Distinct()
                        .ToList()
                );

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.RolesFetchedSuccessfully,
                groupedRoles);
        }

        #region HelperMethod

        private async Task AddResourcesWithRolesAsync(
            OESGroup group,
            List<ResourceWithRolesDto> resourcesWithRoles
        )
        {
            var resourceRepo = _unitOfWork.Repository<OESResource, long>();
            var groupResourceRepo = _unitOfWork.Repository<OESGroupResource, long>();
            var groupResourceRoleRepo = _unitOfWork.Repository<OESGroupResourceRole, long>();
            var roleRepo = _unitOfWork.Repository<OESRole, long>();

            foreach (var res in resourcesWithRoles)
            {
                var resource = await resourceRepo.GetObjAsync(r => r.Name == res.ResourceType.ToString() && !r.IsDeleted);

                if (resource == null)
                {
                    resource = new OESResource
                    {
                        Name = res.ResourceType.ToString(),
                        Description = string.Format(Resource.ResourceDescriptionFormat, res.ResourceType),
                        CreationUser = DefaultSystemUser.Name
                    };

                    await resourceRepo.AddAsync(resource);
                    await _unitOfWork.Complete();
                }

                var groupResource = new OESGroupResource
                {
                    GroupId = group.Id,
                    ResourceId = resource.Id,
                    ResourceType = res.ResourceType,
                    CreationUser = DefaultSystemUser.Name
                };

                await groupResourceRepo.AddAsync(groupResource);
                await _unitOfWork.Complete();

                var roles = await roleRepo.GetAllAsync(r => !r.IsDeleted && res.Roles.Contains(r.Name));

                if (roles?.Any() == true)
                {
                    var roleAssignments = roles.Select(role => new OESGroupResourceRole
                    {
                        GroupResourceId = groupResource.Id,
                        RoleId = role.Id,
                        CreationUser = DefaultSystemUser.Name
                    }).ToList();

                    groupResourceRoleRepo.AddRangAsync(roleAssignments);
                    await _unitOfWork.Complete();
                }
            }
        }

        private async Task AddGroupRolesAsync(Guid groupId, List<ResourceWithRolesDto> resourcesWithRoles)
        {
            var roleRepo = _unitOfWork.Repository<OESRole, long>();
            var groupRoleRepo = _unitOfWork.Repository<OESGroupRole, long>();

            var rolesToInsert = resourcesWithRoles
                .SelectMany(r => r.Roles.Select(roleName => new { r.ResourceType, RoleName = roleName }))
                .ToList();

            if (rolesToInsert.Count == 0)
                return;

            var roleNames = rolesToInsert.Select(r => r.RoleName).Distinct().ToList();

            var dbRoles = await roleRepo.GetAllAsync(r =>
                !r.IsDeleted && roleNames.Contains(r.Name));

            if (!dbRoles.Any())
                return;

            var assignments = new List<OESGroupRole>();

            foreach (var role in dbRoles)
            {
                assignments.Add(new OESGroupRole
                {
                    OESGroupId = groupId,
                    OESRoleId = role.Id,
                    CreationUser = DefaultSystemUser.Name
                });
            }

            if (assignments.Count > 0)
            {
                groupRoleRepo.AddRangAsync(assignments);
                await _unitOfWork.Complete();
            }
        }

        #endregion
    }
}
