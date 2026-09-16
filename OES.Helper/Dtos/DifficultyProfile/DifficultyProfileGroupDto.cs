namespace OES.Helper.Dtos.DifficultyProfile
{
    public class DifficultyProfileGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
