using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.QuestionChoices;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class QuestionDetailsDto
    {
        public long Id { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string? Instructions { get; set; } = string.Empty;

        public string ModelAnswer { get; set; } = string.Empty;

        public List<ChoiceDataDto> Choices { get; set; } = [];

        public long QuestionMetadataId { get; set; }

        public long LanguageId { get; set; }

        public bool UseArabicNumbers { get; set; }

        public bool HasShuffled { get; set; } = false;

        public string? AttachmentFileName { get; set; }

        public long? MaxWords { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public FileUploadSettingsDto? FileUploadSettings { get; set; }

        public bool IsUsedInExam { get; set; } = false;
    }
}
