namespace OES.Helper.Dtos.QualityCheckCommittee
{
    public class QualityCheckCommitteeMemberDto
    {
        public Guid UserId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool IsActive { get; set; }
    }
}
