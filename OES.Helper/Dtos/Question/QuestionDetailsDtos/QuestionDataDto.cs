using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using SharedHelper.General;

namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class QuestionDataDto
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public string? Instructions { get; set; }
        public List<ChoiceDataDto>? Choices { get; set; }
        public string ModelAnswer { get; set; }
        public long QuestionMetadataId { get; set; }
        public long LanguageId { get; set; }
        public bool HasShuffled { get; set; }
        public bool HasAttachment { get; set; }
        public string AttachmentFileName { get; set; }
        public long? MaxWords { get; set; }
        public string AttachmentFileUrl => HasAttachment
            ? $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={AttachmentFileName}"
            : null;
        public int MaxRecordingTimeInSeconds { get; set; }
        public FileUploadSettingsDto? FileUploadSettings { get; set; }
        public bool IsUsedInExam { get; set; }
        public SegmentQuestionConfigDto? SegmentQuestionConfig { get; set; }
    }
}
