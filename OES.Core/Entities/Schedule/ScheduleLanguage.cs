using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class ScheduleLanguage : BaseEntity<long>
    {
        public long ScheduleMetadataId { get; set; }

        public long LanguageId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(ScheduleMetadataId))]
        public virtual ScheduleMetadata ScheduleMetadata { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
    }
}
