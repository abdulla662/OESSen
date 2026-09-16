using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos
{
    public class SegmentQuestionMetaDataDto
    {
        [Range(0, int.MaxValue)]
        public long ParentId { get; set; }

        public string Code { get; set; } = string.Empty;
    }
}
