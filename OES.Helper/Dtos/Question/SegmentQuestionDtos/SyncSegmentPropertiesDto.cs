using SharedHelper.Enums;

namespace OES.Helper.Dtos.Question.SegmentQuestionDtos
{
    public class SyncSegmentPropertiesDto
    {
        public long Id { get; set; }

        public long? ThinkingTime { get; set; }

        public long? ResponseTime { get; set; }

        public long? WordsCount { get; set; }

        public long OrderNumber { get; set; }

        public bool HasScore { get; set; }

        public string? SegmentAudioUrl { get; set; }

        public SegmentQuestionResponseType SegmentQuestionResponseType { get; set; }
    }
}
