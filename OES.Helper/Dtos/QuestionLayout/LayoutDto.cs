namespace OES.Helper.Dtos.QuestionLayout
{
    public class LayoutDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ComponentName { get; set; }
        public long QuestionTypeId { get; set; }
    }
}
