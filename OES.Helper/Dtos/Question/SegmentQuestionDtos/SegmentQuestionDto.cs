using OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos;

namespace OES.Helper.Dtos.Question.SegmentQuestionDtos
{
    public sealed class SegmentQuestionDto
    {
        public SegmentQuestionMetaDataDto SegmentMetaDataDto { get; set; } = new();

        public SegmentQuestionDetailsDto SegmentQuestionDetailsDto { get; set; } = new();

        public SegmentQuestionConfigDto SegmentQuestionConfigDto { get; set; } = new();
    }
}
