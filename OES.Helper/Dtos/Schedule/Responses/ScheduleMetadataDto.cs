using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.Schedule.Responses
{
    public class ScheduleMetadataDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public ScheduleLocation? ScheduleLocation { get; set; } = Enums.ScheduleLocation.Central;

        public PublishingStatus PublishingStatus { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        public List<long> LanguageIds { get; set; } = [];

        public List<long> ExamVenueIds { get; set; } = [];

        public List<VenueSyncData>? ExamVenueNames { get; set; } = [];

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}