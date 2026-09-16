using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long PaperId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }
    }
}
