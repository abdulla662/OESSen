using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionsChoices : BaseEntity<long>
    {
        public string ChoiceText { get; set; }
        public bool IsCorrectAnswer { get; set; }
        public string? AttachmentFileName { get; set; }
        public int OrderId { get; set; }
        public long QuestionDetailsId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(QuestionDetailsId))]
        public QuestionDetails QuestionDetails { get; set; }
    }
}
