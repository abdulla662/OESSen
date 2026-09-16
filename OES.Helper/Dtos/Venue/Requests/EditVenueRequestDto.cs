using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Venue.Requests
{
    public class EditVenueRequestDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CodeIsRequired))]
        public string Code { get; set; }

        public string? DisplayName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.AddressRequired))]
        public string Address { get; set; }

        public string GeoLocation { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.PinCodeRequired))]
        public string PinCode { get; set; }

        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailFormatInvalid))]
        [StringLength(100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailMaxLength))]
        public string VenueEmail { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MobileIsRequired))]
        public string Mobile { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CoordinatorPasswordRequired))]
        [DataType(DataType.Password)]
        public string CoordinatorVenuePassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CoordinatorFullNameIsRequired))]
        public string CoordinatorFullName { get; set; }

        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailFormatInvalid))]
        [StringLength(100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.EmailMaxLength))]
        public string CoordinatorEmail { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MobileIsRequired))]
        [RegularExpression(RegularExpressions.RegularExpressions.GeneralMobileNumber, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.InvalidPhoneNumberFormat))]
        public string CoordinatorMobile { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.IPAddressIsRequired))]
        [RegularExpression(RegularExpressions.RegularExpressions.IPCheck, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.IPAddressInvalidFormat))]
        public string IPAddress { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.UrlRequired))]
        [RegularExpression(RegularExpressions.RegularExpressions.UrlCheck, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.UrlInvalidFormat))]
        public string Url { get; set; }

        public bool IsPBT { get; set; } = false;

        public string TCIds { get; set; }
    }
}
