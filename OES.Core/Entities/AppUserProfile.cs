using SharedHelper.Enums;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class AppUserProfile : BaseEntity<Guid>
    {
        [StringLength(maximumLength: 150, ErrorMessage = "Length between 5 and 150 char", MinimumLength = 5)]
        [Required(ErrorMessage = "This Field is required")]
        public string Username { get; set; }

        [StringLength(maximumLength: 150, ErrorMessage = "Length between 5 and 150 char", MinimumLength = 5)]
        [Required(ErrorMessage = "This Field is required")]
        public string EmailAddress { get; set; }

        public Gender? Gender { get; set; }

        public ICollection<AppUserProfileSubject> AppUserProfileSubjects { get; set; } = new List<AppUserProfileSubject>();
    }
}
