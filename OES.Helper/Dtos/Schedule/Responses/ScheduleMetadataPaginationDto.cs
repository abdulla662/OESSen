using OES.Helper.Enums;
namespace OES.Helper.Dtos.Schedule.Responses
{
    public class ScheduleMetadataPaginationDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public ScheduleLocation? ScheduleLocation { get; set; }

        public string ScheduleLocationDisplay => ScheduleLocation?.ToLocalizedString();

        public PublishingStatus PublishingStatus { get; set; }

        public string PublishingStatusDisplay => PublishingStatus.ToLocalizedString();

        public DateOnly StartDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly EndTime { get; set; }

        public SyncingStatus SyncStatus { get; set; }

        public string SyncStatusDisplay => SyncStatus.ToLocalizedString();

        public List<long> LanguageIds { get; set; }

        public List<long> ExamVenueIds { get; set; }
    }
}