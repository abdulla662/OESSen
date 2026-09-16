namespace OES.Helper.Dtos.EquationTemplate
{
    public class EquationGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}