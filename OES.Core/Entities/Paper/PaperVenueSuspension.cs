using OES.Core.Entities.Schedule;
using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperVenueSuspension : BaseEntity<long>
    {
        public long PaperId { get; set; }

        public long VenueId { get; set; }

        public AvailabilityStatus Status { get; set; }


        // Navigational properties

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(VenueId))]
        public virtual Venue Venue { get; set; }
    }
}
