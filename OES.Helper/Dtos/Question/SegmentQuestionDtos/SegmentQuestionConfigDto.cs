using SharedHelper.Enums;

namespace OES.Helper.Dtos.Question.SegmentQuestionDtos
{
    public sealed class SegmentQuestionConfigDto
    {
        public long? ThinkingTime { get; set; } = 0;

        public long? ResponseTime { get; set; } = 0;

        public long? WordsCount { get; set; } = 0;

        public long OrderNumber { get; set; }

        public bool HasScore { get; set; }

        public string SegmentAudioUrl { get; set; } = string.Empty;

        public SegmentQuestionResponseType SegmentQuestionResponseType { get; set; }
    }
}