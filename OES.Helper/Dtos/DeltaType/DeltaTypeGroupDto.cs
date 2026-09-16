namespace OES.Helper.Dtos.DeltaType
{
    public class DeltaTypeGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
