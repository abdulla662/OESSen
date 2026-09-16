using OES.Helper.Enums;

namespace OES.Helper.Dtos.UploadFiles
{
    public class UploadMediaFileDto
    {
        public string Uri { get; set; }
        public MediaType MediaType { get; set; }
        public long? QuestionDetailsId { get; set; }
    }
}
