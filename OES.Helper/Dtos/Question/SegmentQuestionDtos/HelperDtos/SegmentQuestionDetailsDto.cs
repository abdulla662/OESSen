using OES.Helper.Dtos.QuestionChoices;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos
{
    public class SegmentQuestionDetailsDto
    {
        public long Id { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public List<ChoiceDataDto> Choices { get; set; } = [];

        public string ModelAnswer { get; set; } = string.Empty;

        public string MediaFileName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public long LanguageId { get; set; }

        public long QuestionMetadataId { get; set; }

        public long? MaxWords { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public long? SegmentQuestionPropertyId { get; set; }

        public bool IsUsedInExam { get; set; }
    }
}
