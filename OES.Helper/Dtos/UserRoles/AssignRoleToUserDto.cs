namespace OES.Helper.Dtos.UserRoles
{
    public class AssignRoleToUserDto
    {
        public Guid UserID { get; set; }

        public List<Guid> RoleIDs { get; set; }
    }
}
