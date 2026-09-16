using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class AddOrUpdateCandidateRequestDto
    {
        public long CandidateId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CandidateCodeIsRequired))]
        public string CandidateCode { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        [StringLength(100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameCannotBeLonger))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.UserNameIsRequired))]

        [StringLength(50, MinimumLength = 3, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.UserNameMustBeBetween))]
        public string UserName { get; set; }

        [RegularExpression(RegularExpressions.RegularExpressions.NationalNumber, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NationaIIdMustBeNumbersOnly))]
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NationaIIdIsRequired))]
        public string NationalId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.PasswordIsRequired))]

        [MinLength(6, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.PasswordMustBe))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [StringLength(100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.QualificationCannotBeLonger))]
        public string Qualification { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.AddressCannotBeLonger))]
        public string Address { get; set; }

        [RegularExpression(RegularExpressions.RegularExpressions.InternationalPhoneNumber, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MobileInvalid))]
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MobileIsRequired))]
        public string Mobile { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailIsRequired))]

        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailMustBe))]

        [StringLength(100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.Emailcannotbelonger))]
        public string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.GenderIsRequired))]

        [Range(0, 1, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.GenderMustBeEither))]
        public int Gender { get; set; }

        [StringLength(20, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RegistrationCenterCodeCannot))]
        public string RegistrationCenterCode { get; set; }

        [DataType(DataType.DateTime)]
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RegistrationDateTimeIsRequired))]
        public DateTime RegistrationDateTime { get; set; } = DateTimeHelper.Now;

        public IBrowserFile PhotoFile { get; set; }

        public IBrowserFile SignatureFile { get; set; }

        public string PhotoURL { get; set; }

        public string SignatureURL { get; set; }

        public bool HasDisability { get; set; } // NOTE: This field is planned to be used in ministry version

        public long? DisabilityId { get; set; } // NOTE: This field is planned to be used in ministry version
    }
}