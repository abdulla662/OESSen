using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperStageCategoryDecisionPath : BaseEntity<long>
    {
        public long? StageId { get; set; }

        public long PaperId { get; set; }

        public long QuestionCategoryId { get; set; }

        public decimal? DecisionPathValue { get; set; }

        public decimal? FixedDPath { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(StageId))]
        public virtual Stage Stage { get; set; }

        [ForeignKey(nameof(QuestionCategoryId))]
        public virtual QuestionCategory QuestionCategory { get; set; }

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }
    }
}
