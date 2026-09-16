
namespace OES.Helper.Dtos.FileUploadResponseSettings
{
    public class FileUploadSettingsResponseDto
    {
        public long QuestionMetadataId { get; set; }

        public bool ShowAnswerTextArea { get; set; }

        public string SupportedFileExtensions { get; set; }

        public int UploadedFilesCount { get; set; }

        public int SingleFileMaxSizeInMB { get; set; }
    }
}
