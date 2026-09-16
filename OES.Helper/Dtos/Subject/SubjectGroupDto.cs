namespace OES.Helper.Dtos.Subject
{
    public class SubjectGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
