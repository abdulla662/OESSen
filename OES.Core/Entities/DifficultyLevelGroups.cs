using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class DifficultyLevelGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long DifficultyLevelId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(DifficultyLevelId))]
        public virtual DifficultyLevel DifficultyLevel { get; set; }
    }
}
