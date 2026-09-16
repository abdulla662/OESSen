namespace OES.Helper.Dtos.ILO
{
    public class IloGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
