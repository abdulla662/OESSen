using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using SharedHelper.General;

namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidateDetailsDto
    {
        public long CandidateId { get; set; }

        [Required(ErrorMessage = "Candidate Code is required")]
        public string CandidateCode { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "NationalId is required")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [StringLength(100, ErrorMessage = "Qualification cannot be longer than 100 characters")]
        public string Qualification { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Mobile is required")]
        public string Mobile { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessage = "Email must be in the format test@domain.com")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [Range(0, 1, ErrorMessage = "Gender must be either 0 (Male) or 1 (Female)")]
        public int Gender { get; set; }

        [StringLength(20, ErrorMessage = "Registration Center Code cannot be longer than 20 characters")]
        public string RegistrationCenterCode { get; set; }

        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "RegistrationDateTime is required")]
        public DateTime RegistrationDateTime { get; set; } = DateTimeHelper.Now;

        public IBrowserFile? PhotoFile { get; set; }

        public IBrowserFile? SignatureFile { get; set; }
    }
}
