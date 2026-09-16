namespace OES.Helper.Dtos
{
    public class LanguageGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
