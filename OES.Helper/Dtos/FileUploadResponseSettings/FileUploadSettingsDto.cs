
namespace OES.Helper.Dtos.FileUploadResponseSettings
{
    public class FileUploadSettingsDto
    {
        public long? Id { get; set; }

        public long QuestionMetadataId { get; set; }

        public bool ShowAnswerTextArea { get; set; } = false;

        public string SupportedFileExtensions { get; set; }

        public int UploadedFilesCount { get; set; } = 1;

        public int SingleFileMaxSizeInMB { get; set; } = 1;
    }
}
