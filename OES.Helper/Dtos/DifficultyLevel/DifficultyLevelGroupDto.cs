namespace OES.Helper.Dtos.DifficultyLevel
{
    public class DifficultyLevelGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
