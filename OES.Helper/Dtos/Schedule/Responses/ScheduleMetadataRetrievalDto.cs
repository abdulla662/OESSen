using OES.Helper.Enums;

namespace OES.Helper.Dtos.Schedule.Responses
{
    public class ScheduleMetadataRetrievalDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public ScheduleLocation? ScheduleLocation { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        public List<long> LanguageIds { get; set; } = [];

        public List<long> ExamVenueIds { get; set; } = [];
    }
}
