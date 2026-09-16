namespace OES.Core.Entities
{
    public class QuestionTemplate : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Data { get; set; }

        public bool IsFromQuestionAI { get; set; }
    }
}
