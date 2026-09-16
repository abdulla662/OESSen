using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class TransitionProfileGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long ProfileId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public virtual TransitionProfile TransitionProfile { get; set; }
    }
}
