using OES.Core.Entities.Paper;

namespace OES.Core.Entities
{
    public class DeltaType : BaseEntity<long>
    {
        public string Name { get; set; }

        public virtual ICollection<DifficultyLevel> DifficultyLevels { get; set; } = [];

        public virtual ICollection<Block> Blocks { get; set; } = [];
    }
}
