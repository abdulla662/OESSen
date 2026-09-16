using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations.Schema;


namespace OES.Core.Entities
{
    public class BlockGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long BlockId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(BlockId))]
        public virtual Block Block { get; set; }
    }
}
