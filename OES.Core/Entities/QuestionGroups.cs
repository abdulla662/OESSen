using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long QuestionId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionMetadata Question { get; set; }
    }
}
