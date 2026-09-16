using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Questionlanguage
{
    public class LanguageUpdateDto
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, AllowedValues("RTL", "LTR")]
        public string LanguageDirection { get; set; }
    }
}
