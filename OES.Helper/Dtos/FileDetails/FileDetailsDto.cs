namespace OES.Helper.Dtos.FileDetails
{
    public class FileDetailsDto
    {
        public string FileName { get; set; }
        public string Authors { get; set; }
        public long? Pages { get; set; }
        public long Size { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public long? WordCount { get; set; }
        public long? LineCount { get; set; }
        public string FileType { get; set; }
    }
}
