namespace OES.Helper.Dtos.QuestionCategory
{
    public class QuestionCategoryGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
