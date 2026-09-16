using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Venue.Requests
{
    public class AddVenueRequestDto
    {
        [Required(ErrorMessageResourceName = nameof(Resource.NameRequired), ErrorMessageResourceType = typeof(Resource))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.RootCodeRequired), ErrorMessageResourceType = typeof(Resource))]
        public string Code { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.AddressRequired), ErrorMessageResourceType = typeof(Resource))]
        public string DisplayName { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.AddressRequired), ErrorMessageResourceType = typeof(Resource))]
        public string Address { get; set; }

        public string GeoLocation { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.PinCodeRequired), ErrorMessageResourceType = typeof(Resource))]
        public string PinCode { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.VenueEmailRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceName = nameof(Resource.VenueEmailInvalid), ErrorMessageResourceType = typeof(Resource))]
        [StringLength(100, ErrorMessageResourceName = nameof(Resource.VenueEmailTooLong), ErrorMessageResourceType = typeof(Resource))]
        public string VenueEmail { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.MobileRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.InternationalPhoneNumber, ErrorMessageResourceName = nameof(Resource.MobileInvalid), ErrorMessageResourceType = typeof(Resource))]
        public string Mobile { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.CoordinatorPasswordRequired), ErrorMessageResourceType = typeof(Resource))]
        [DataType(DataType.Password)]
        public string CoordinatorVenuePassword { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.CoordinatorFullNameRequired), ErrorMessageResourceType = typeof(Resource))]
        public string CoordinatorFullName { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.CoordinatorEmailRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.EmailExpression, ErrorMessageResourceName = nameof(Resource.CoordinatorEmailInvalid), ErrorMessageResourceType = typeof(Resource))]
        [StringLength(100, ErrorMessageResourceName = nameof(Resource.CoordinatorEmailTooLong), ErrorMessageResourceType = typeof(Resource))]
        public string CoordinatorEmail { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.CoordinatorMobileRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.GeneralMobileNumber, ErrorMessageResourceName = nameof(Resource.CoordinatorMobile), ErrorMessageResourceType = typeof(Resource))]
        public string CoordinatorMobile { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.IPAddressRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.IPCheck, ErrorMessageResourceName = nameof(Resource.IPAddressInvalid), ErrorMessageResourceType = typeof(Resource))]
        public string IPAddress { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.UrlRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.UrlCheck, ErrorMessageResourceName = nameof(Resource.UrlInvalid), ErrorMessageResourceType = typeof(Resource))]
        public string Url { get; set; }

        public bool IsPBT { get; set; } = false;

        public string TCIds { get; set; }
    }
}
