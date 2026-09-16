using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperFormBlock : BaseEntity<long>
    {
        public long BlockId { get; set; }

        public long FormId { get; set; }

        public long PaperId { get; set; }

        public long? AdaptiveSectionId { get; set; }

        public bool Distribution { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(BlockId))]
        public virtual Block Block { get; set; }

        [ForeignKey(nameof(AdaptiveSectionId))]
        public virtual AdaptiveSection AdaptiveSection { get; set; }

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(FormId))]
        public virtual PaperForm Form { get; set; }
    }
}
