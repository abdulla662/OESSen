namespace OES.Helper.Dtos.QualityCheckCommittee
{
    public class QualityCheckCommitteeGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
