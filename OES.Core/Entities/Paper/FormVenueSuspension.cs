using OES.Core.Entities.Schedule;
using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class FormVenueSuspension : BaseEntity<long>
    {
        public long FormId { get; set; }

        public long VenueId { get; set; }

        public AvailabilityStatus Status { get; set; }


        // Navigational properties

        [ForeignKey(nameof(FormId))]
        public virtual PaperForm Form { get; set; }

        [ForeignKey(nameof(VenueId))]
        public virtual Venue Venue { get; set; }
    }
}
