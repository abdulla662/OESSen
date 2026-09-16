using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.ScheduleSummary
{
    public class GetSchedulePaperSummaryDto
    {
        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public string PaperCode { get; set; }

        public PaperType PaperType { get; set; }

        public AdaptivePaperSubtype AdaptiveSubtype { get; set; }

        public QuestionSelectionType PaperQuestionSelectionType { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public string SchedulePaperDescription { get; set; }
    }
}
