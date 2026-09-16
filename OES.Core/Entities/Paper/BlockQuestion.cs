using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class BlockQuestion : BaseEntity<long>
    {
        public long QuestionMetadataId { get; set; }

        public long BlockId { get; set; }

        [ForeignKey(nameof(QuestionMetadataId))]
        public virtual QuestionMetadata QuestionMetadata { get; set; }

        [ForeignKey(nameof(BlockId))]
        public virtual Block Block { get; set; }
    }
}
