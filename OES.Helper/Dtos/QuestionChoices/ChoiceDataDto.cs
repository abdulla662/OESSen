using SharedHelper.General;

namespace OES.Helper.Dtos.QuestionChoices
{
    public class ChoiceDataDto
    {
        public long Id { get; set; }

        public string ChoiceText { get; set; }

        public bool IsCorrectAnswer { get; set; }

        public int OrderId { get; set; }

        public bool HasAttachment { get; set; }

        public string? AttachmentFileName { get; set; }

        public string AttachmentFileUrl => HasAttachment
            ? $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={AttachmentFileName}"
            : null;
    }
}
