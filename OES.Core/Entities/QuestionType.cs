using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class QuestionType : BaseEntity<long>
    {
        [Required]
        public string Name { get; set; }

        public bool IsAutoCorrectable { get; set; }


        // Navigational Properties

        public virtual ICollection<QuestionLayout> QuestionTypes { get; set; } = [];

        public virtual ICollection<AutoPaperItemBankQuestionSection> ItemBankPointQuestions { get; set; } = [];
    }
}
