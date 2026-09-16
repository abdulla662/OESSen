using OES.Helper.Enums;

namespace OES.Helper.Dtos.ScheduleSummary
{
    public class GetScheduleSummaryDto
    {
        public long ScheduleMetadataId { get; set; }

        public string ScheduleName { get; set; }

        public string ScheduleCode { get; set; }

        public string ScheduleDescription { get; set; }

        public DateOnly ScheduleStartDate { get; set; }

        public DateOnly ScheduleEndDate { get; set; }

        public TimeOnly ScheduleStartTime { get; set; }

        public TimeOnly ScheduleEndTime { get; set; }

        public ScheduleLocation ScheduleLocation { get; set; }

        public PublishingStatus SchedulePublishingStatus { get; set; }

        public List<GetSchedulePaperSummaryDto> Papers { get; set; }
    }
}