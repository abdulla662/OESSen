using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.User
{
    public class LoginDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailIsRequired))]
        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailFormatInvalid))]
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public long OrganizationId { get; set; }

        public string ModuleName { get; set; }
    }
}
