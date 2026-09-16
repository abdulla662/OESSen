using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class DifficultyProfileGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long DifficultyProfileId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(DifficultyProfileId))]
        public virtual DifficultyProfile DifficultyProfile { get; set; }
    }
}
