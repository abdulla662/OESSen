namespace OES.Helper.PagesEndpointsRolesDtos
{
    public class AssignRoleToPageDTO
    {
        public long PageID { get; set; }

        public List<Guid> RoleIDs { get; set; }
    }
}
