using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class StandardSection : BaseEntity<long>
    {
        public string Name { get; set; }

        public string SectioningIdentifier { get; set; }

        public long PaperId { get; set; }

        public bool IsRestrictedTime { get; set; }

        public double TimeInMinutes { get; set; }

        public bool IsRandom { get; set; }

        public int OrderId { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        public long? FormId { get; set; } // This is a nullable field to allow for sections that don't have a form but linked directly with paper, CURRENTLY AUTO PAPER.


        // Relationships

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(FormId))]
        public virtual PaperForm Form { get; set; }

        [ForeignKey(nameof(InstructionSectionTemplateId))]
        public virtual Template InstructionSectionTemplate { get; set; }

        public virtual ICollection<ManualPaperItemBankQuestionSection> ManualQuestions { get; set; } = [];

        public virtual ICollection<AutoPaperItemBankQuestionSection> AutoQuestions { get; set; } = [];
    }
}
