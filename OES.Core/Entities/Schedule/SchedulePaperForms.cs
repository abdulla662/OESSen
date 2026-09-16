using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class SchedulePaperForms : BaseEntity<long>
    {
        public long FormId { get; set; }
        public long SchedulePaperId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(SchedulePaperId))]
        public SchedulePaper SchedulePaper { get; set; }
    }
}
