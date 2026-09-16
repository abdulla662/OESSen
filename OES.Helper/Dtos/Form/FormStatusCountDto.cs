using OES.Helper.Enums;

namespace OES.Helper.Dtos.Form
{
    public class FormStatusCountDto
    {
        public AvailabilityStatus Status { get; set; }

        public string StatusDisplay => Status.ToLocalizedString();

        public int Count { get; set; }
    }
}
