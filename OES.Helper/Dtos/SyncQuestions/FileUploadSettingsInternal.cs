namespace OES.Helper.Dtos.SyncQuestions
{
    public class FileUploadSettingsInternal
    {
        public bool ShowAnswerTextArea { get; set; }
        public string SupportedFileExtensions { get; set; }
        public int UploadedFilesCount { get; set; }
        public int SingleFileMaxSizeInMB { get; set; }
        public long QuestionMetadataId { get; set; }
    }
}
