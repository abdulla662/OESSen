namespace OES.Helper.Dtos.FileDetails
{
    public class UploadedFileAndSimilarities
    {
        public string UploadedFileName { get; set; }
        public List<FileNameAndProbability> FileNameAndProbabilities { get; set; }
    }
}
