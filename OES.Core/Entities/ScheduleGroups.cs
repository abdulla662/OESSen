using OES.Core.Entities.Schedule;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ScheduleGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long ScheduleId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(ScheduleId))]
        public virtual ScheduleMetadata Schedule { get; set; }
    }
}
