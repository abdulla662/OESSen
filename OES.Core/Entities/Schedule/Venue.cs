using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities.Schedule
{
    public class Venue : BaseEntity<long>
    {
        [Required] public string Name { get; set; }

        [Required] public string Code { get; set; }

        public string? DisplayName { get; set; }

        [Required] public string TCIds { get; set; }

        [Required] public string Address { get; set; }

        public string GeoLocation { get; set; }

        [Required] public string PinCode { get; set; }

        [Required] public string VenueEmail { get; set; }

        [Required] public string Mobile { get; set; }

        [Required] public string CoordinatorVenuePassword { get; set; }

        [Required] public string CoordinatorFullName { get; set; }

        [Required] public string CoordinatorEmail { get; set; }

        [Required] public string CoordinatorMobile { get; set; }

        [Required] public string IPAddress { get; set; }

        [Required] public string Url { get; set; }

        public bool IsPBT { get; set; } = false;


        // Navigational Properties

        public virtual ICollection<ScheduleVenue> Schedules { get; set; }

        public virtual ICollection<SchedulePaperCandidate> Candidates { get; set; }

        public virtual ICollection<VenueGroups> VenueGroups { get; set; } = [];
    }
}
