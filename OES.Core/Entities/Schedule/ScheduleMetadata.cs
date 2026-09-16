using OES.Helper.Enums;

namespace OES.Core.Entities.Schedule
{
    public class ScheduleMetadata : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public ScheduleLocation ScheduleLocation { get; set; }

        public PublishingStatus PublishingStatus { get; set; }

        public SyncingStatus SyncStatus { get; set; }

        public bool IsAutoSyncEnabled { get; set; }

        public TimeOnly? SyncScheduleTime { get; set; }


        // Navigational Properties

        public ICollection<SchedulePaper> Papers { get; set; } = [];

        public ScheduleSecurityConfiguration SecurityConfiguration { get; set; }

        public ICollection<ScheduleVenue> Venues { get; set; } = [];

        public ICollection<ScheduleLanguage> Languages { get; set; } = [];

        public virtual ICollection<ScheduleGroups> ScheduleGroups { get; set; } = [];
    }
}