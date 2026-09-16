using OES.Core.Entities.Schedule;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class VenueGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long VenueId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(VenueId))]
        public virtual Venue Venue { get; set; }
    }
}
