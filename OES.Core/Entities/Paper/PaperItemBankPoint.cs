using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperItemBankPoint : BaseEntity<long>
    {
        public long PaperId { get; set; }

        public long ItemBankId { get; set; }

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(ItemBankId))]
        public virtual ItemBank ItemBank { get; set; }


        // Navigational Properties

        public virtual ICollection<ManualPaperItemBankQuestionSection> ManualPaperItemBankQuestionSections { get; set; }

        public virtual ICollection<AutoPaperItemBankQuestionSection> AutoPaperItemBankQuestionSections { get; set; }
    }
}
