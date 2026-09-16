using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class GeneratedFormQuestion : BaseEntity<long>
    {
        public long? FormId { get; set; }

        public long? QuestionId { get; set; }

        public PaperQuestionStatus FormQuestionStatus { get; set; }

        public double? Score { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(FormId))]
        public virtual PaperForm Form { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionMetadata Question { get; set; }
    }
}