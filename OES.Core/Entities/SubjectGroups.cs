using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class SubjectGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long SubjectId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; }
    }
}
