using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class PaperItemBankEquation : BaseEntity<long>
    {
        public long PaperId { get; set; }

        public long ItemBankId { get; set; }

        public long FormId { get; set; }

        public long EquationTemplateId { get; set; }

        public long EquationCategoryId { get; set; }


        // Navigation properties

        [ForeignKey(nameof(PaperId))]
        public PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(ItemBankId))]
        public ItemBank ItemBank { get; set; }

        [ForeignKey(nameof(FormId))]
        public PaperForm Form { get; set; }

        [ForeignKey(nameof(EquationTemplateId))]
        public EquationTemplate EquationTemplate { get; set; }

        [ForeignKey(nameof(EquationCategoryId))]
        public EquationCategory EquationCategory { get; set; }
    }
}
