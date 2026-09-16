using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Core.Entities.Schedule.Views
{
    public class SchedulePaperView
    {
        // Schedule Properties

        public long ScheduleMetadataId { get; set; }

        public string ScheduleName { get; set; }

        public string ScheduleCode { get; set; }

        public string ScheduleDescription { get; set; }

        public DateTime ScheduleStartDate { get; set; }

        public DateTime ScheduleEndDate { get; set; }

        public TimeOnly ScheduleStartTime { get; set; }

        public TimeOnly ScheduleEndTime { get; set; }

        public ScheduleLocation ScheduleLocation { get; set; }

        public PublishingStatus SchedulePublishingStatus { get; set; }


        // Paper Properties

        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public string PaperCode { get; set; }

        public PaperType PaperType { get; set; }

        public string SchedulePaperDescription { get; set; }

        public DateTime PaperStartDate { get; set; }

        public DateTime PaperEndDate { get; set; }

        public TimeOnly PaperStartTime { get; set; }

        public TimeOnly PaperEndTime { get; set; }

        public AdaptivePaperSubtype AdaptiveSubtype { get; set; }

        public QuestionSelectionType PaperQuestionSelectionType { get; set; }
    }
}