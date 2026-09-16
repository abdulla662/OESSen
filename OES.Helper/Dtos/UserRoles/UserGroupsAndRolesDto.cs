using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Helper.Dtos.UserRoles
{
    public class UserGroupsAndRolesDto
    {
        public Guid GroupId { get; set; }

        public string GroupName { get; set; }

        public List<RoleDto> GroupRoles { get; set; } = [];
    }
}
