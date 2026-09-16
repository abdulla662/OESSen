using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class ManualPaperItemBankQuestionSection : BaseEntity<long>
    {
        public long ItemBankPointId { get; set; }

        public long QuestionMetaDataId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long? SectionId { get; set; }

        public PaperQuestionStatus PaperQuestionStatus { get; set; } = PaperQuestionStatus.Used;


        // Navigational Properties

        [ForeignKey(nameof(ItemBankPointId))]
        public virtual PaperItemBankPoint ItemBankPoint { get; set; }

        [ForeignKey(nameof(DifficultyLevelId))]
        public virtual DifficultyLevel DifficultyLevel { get; set; }

        [ForeignKey(nameof(QuestionMetaDataId))]
        public virtual QuestionMetadata QuestionMetadata { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual StandardSection Section { get; set; }
    }
}
