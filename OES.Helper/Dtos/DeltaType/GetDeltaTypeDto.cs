
using OES.Helper.Enums;

namespace OES.Helper.Dtos.DeltaType
{
    public class GetDeltaTypeDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string NameDisplay => Name?.ToLocalizedString<DeltaTypes>();

        public string CreationUser { get; set; }
    }
}
