using OES.Helper.Dtos.OESUserGroups;
using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Helper.Dtos.CreateTemplate
{
    public class CreateTemplateDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPredefined { get; set; }

        public bool IsTemplate { get; set; }

        public List<ResourceWithRolesDto> ResourcesWithRoles { get; set; } = new();

        public List<UserResourceRoleDto> UsersAssignments { get; set; } = new();

        public Guid GroupId { get; set; }

        public ResourceType? ResourceType { get; set; }
    }
}
