using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Template.Response
{
    public class TemplateDataDto
    {
        public long Id { get; set; }

        public long TemplateTypeId { get; set; }

        public string TemplateType { get; set; }

        public string TemplateTypeDisplay => TemplateType?.ToLocalizedString<TemplateTypeEnum>();

        [Required]
        public string Name { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
