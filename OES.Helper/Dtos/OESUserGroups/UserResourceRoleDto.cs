using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Helper.Dtos.OESUserGroups
{
    public class UserResourceRoleDto
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public ResourceType ResourceType { get; set; }

        public List<string> Roles { get; set; } = [];
    }
}
