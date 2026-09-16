using OES.Helper.Dtos.OESRole;

namespace OES.Helper.Dtos.OESUserGroups
{
    public class OESGroupDetailsDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<OESRoleDto> RoleDtos { get; set; } = [];
    }
}
