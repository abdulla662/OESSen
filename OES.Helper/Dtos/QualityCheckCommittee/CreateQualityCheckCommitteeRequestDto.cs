using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.QualityCheckCommittee
{
    public class CreateQualityCheckCommitteeRequestDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeNameIsrequired))]
        [MaxLength(200, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameCannotExceed200Chars))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeDescriptionIsRequired))]
        [MaxLength(200, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DescriptionCannotExceed200Chars))]
        public string Description { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeChiefIsRequired))]
        public Guid ChiefUserId { get; set; }

        public List<Guid> MemberUserIds { get; set; } = [];

        public List<long> SelectedItemBankIds { get; set; } = [];
    }
}
