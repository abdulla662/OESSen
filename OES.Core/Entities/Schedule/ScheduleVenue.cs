using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class ScheduleVenue : BaseEntity<long>
    {
        public long ScheduleId { get; set; }

        public long VenueId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(ScheduleId))]
        public ScheduleMetadata ScheduleMetadata { get; set; }

        [ForeignKey(nameof(VenueId))]
        public Venue Venue { get; set; }
    }
}
