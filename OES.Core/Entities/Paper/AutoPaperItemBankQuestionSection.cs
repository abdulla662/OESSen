using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class AutoPaperItemBankQuestionSection : BaseEntity<long>
    {
        public long ItemBankPointId { get; set; }

        public long DifficultyLevelID { get; set; }

        public long QuestionTypeID { get; set; }

        public long SelectedCount { get; set; }

        public long SubQuestionCount { get; set; } = 0;

        public long? SectionId { get; set; }

        public string QuestionIds { get; set; } = string.Empty;


        // Navigational Properties

        [ForeignKey(nameof(ItemBankPointId))]
        public PaperItemBankPoint ItemBankPoint { get; set; }

        [ForeignKey(nameof(DifficultyLevelID))]
        public DifficultyLevel DifficultyLevel { get; set; }

        [ForeignKey(nameof(QuestionTypeID))]
        public QuestionType QuestionType { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual StandardSection Section { get; set; }
    }
}
