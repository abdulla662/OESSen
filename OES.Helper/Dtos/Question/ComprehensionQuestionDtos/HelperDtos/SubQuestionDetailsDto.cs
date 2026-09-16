using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Enums;
using SharedHelper.General;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos
{
    public class SubQuestionDetailsDto
    {
        public long Id { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public List<ChoiceDataDto> Choices { get; set; } = [];

        [Required]
        public string ModelAnswer { get; set; } = string.Empty;

        public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentFileName);

        public string AttachmentFileName { get; set; }

        public long? MaxWords { get; set; }

        public string AttachmentFileUrl => HasAttachment
            ? $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={AttachmentFileName}"
            : null;

        [Range(1, int.MaxValue)]
        public long LanguageId { get; set; }

        public long QuestionMetadataId { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public QuestionType QuestionTypeName { get; set; }

        public bool UseArabicNumbers { get; set; }

        public bool HasShuffled { get; set; } = false;

        public bool IsUsedInExam { get; set; }
    }
}
