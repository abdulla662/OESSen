
namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public class QuestionMetadataDto
    {
        public long Id { get; set; }

        public string Code { get; set; }

        public string Body { get; set; }

        public long DifficultyLevelId { get; set; }

        public bool IsRoot { get; set; }

        public long? ParentId { get; set; }

        public int MaximumAnswerTime { get; set; }

        public long QuestionTypeId { get; set; }

        public long ItemBankId { get; set; }

        public long CurrentExhaustionCount { get; set; }

        public long QuestionsExhaustionCount { get; set; }

        public QuestionStatus QuestionStatus { get; set; }
    }
}
