namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public class QuestionGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
