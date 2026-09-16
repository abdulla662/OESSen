namespace OES.Helper.Dtos.Block.Responses
{
    public class BlockGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
