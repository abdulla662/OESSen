using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Template.Request
{
    public class AddTemplateRequestDto
    {
        public long TemplateTypeId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
