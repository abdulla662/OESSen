namespace OES.Helper.Dtos.OESUserGroups
{
    public class OESGroupDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public ICollection<OESGroupRoleDto>? Roles { get; set; }
    }
}
