using SharedHelper.Enums;

namespace OES.Core.Entities
{
    public class SegmentQuestionProperties : BaseEntity<long>
    {
        public long? ThinkingTime { get; set; }

        public long? ResponseTime { get; set; }

        public long? WordsCount { get; set; }

        public long OrderNumber { get; set; }

        public bool HasScore { get; set; }

        public string? SegmentAudioUrl { get; set; }

        public SegmentQuestionResponseType SegmentQuestionResponseType { get; set; }


        // Navigational Properties

        public virtual QuestionDetails QuestionDetails { get; set; }
    }
}
