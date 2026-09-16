using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.Question
{
    public class UploadFreeQuestions
    {
        public IFormFile File { get; set; }
    }
}
