using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Helper.Dtos.CreateTemplate
{
    public class ResourceWithRolesDto
    {
        public ResourceType ResourceType { get; set; }

        public List<string> Roles { get; set; } = [];
    }
}
