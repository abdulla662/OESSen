namespace OES.Helper.PagesEndpointsRolesDtos
{
    public class AssignRoleToEndpointDto
    {
        public long PathID { get; set; }

        public List<Guid> RoleIDs { get; set; }
    }
}
