namespace OES.Helper.Dtos.Candidate
{
    public class CandidateGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
