using OES.Helper.Dtos.ItemBank;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.QualityCheckCommittee
{
    public class QualityCheckCommitteeDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeNameIsRequired))]
        [MaxLength(200, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameCannotExceed200Chars))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeDescriptionIsRequired))]
        [MaxLength(200, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DescriptionCannotExceed200Chars))]
        public string Description { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CommitteeChiefIsRequired))]
        public Guid ChiefId { get; set; }

        public string ChiefName { get; set; }

        public string ChiefEmail { get; set; }

        public RootItemBankDto SelectedRootItemBank { get; set; }

        public List<QualityCheckCommitteeMemberDto> Members { get; set; } = [];

        public List<QualityCheckItemBankDto> ItemBanks { get; set; } = [];
    }
}
