namespace OES.Helper.Dtos.Paper.Responses
{
    public class PaperGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}