using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.UploadFiles
{
    public class QuestionMetadataAdditionWithUploadFile : QuestionMetadataAdditionOrUpdateDto
    {
        [Required]
        public IFormFile FormFile { get; set; }

        public long LanguageId { get; set; }

        public List<string> SelectedTypeNames { get; set; }
    }
}
