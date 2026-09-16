namespace OES.Helper.Dtos.Question
{
    public class QuestionAdvancedFilterDto
    {
        public long? CategoryId { get; set; }
        public long? QuestionTypeId { get; set; }
        public string CreatedByUserName { get; set; }
        public string ModifiedByUserName { get; set; }
        public DateTime? ModifiedFromDate { get; set; }
        public DateTime? ModifiedToDate { get; set; }
    }
}
