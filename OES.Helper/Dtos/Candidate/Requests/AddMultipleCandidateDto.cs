using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class AddMultipleCandidateDto
    {
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

        public string DateOfBirth { get; set; } = DateTime.Today.ToString();

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Mobile is required")]
        public string Mobile { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "Photo URL cannot be longer than 500 characters")]
        public string? PhotoURL { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Signature URL cannot be longer than 500 characters")]
        public string? SignatureURL { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required")]
        [Range(0, 1, ErrorMessage = "Gender must be either 0 (Male) or 1 (Female)")]
        public int Gender { get; set; }

        [Required(ErrorMessage = "Registration Center Code is required")]
        [StringLength(20, ErrorMessage = "Registration Center Code cannot be longer than 20 characters")]
        public string RegistrationCenterCode { get; set; }

        public string RegistrationDateTime { get; set; } = DateTimeHelper.Now.ToString();

        [StringLength(500, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RegistrationNumberMaxlength))]
        public string RegistrationNumber { get; set; } // NOTE: This field is optional when importing candidates alone without linking to any schedule paper and venue.

        public string CandidateExamDate { get; set; } // NOTE: This field is optional when importing candidates alone without linking to any schedule paper and venue.

        public int HasDisability { get; set; } // NOTE: This field is planned to be used in ministry version

        public long? DisabilityId { get; set; } // NOTE: This field is planned to be used in ministry version
    }
}