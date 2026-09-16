using OES.Helper.Dtos.OESUserGroups;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ItemBank
{
    public class EditItemBankNodeDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [MaxLength(120)]
        [MinLength(2)]
        public string Name { get; set; }

        public float Hours { get; set; } = 0;

        public long? ParentId { get; set; }

        public string Description { get; set; } = "";

        public bool IsActive { get; set; }

        public bool ParentIsActive { get; set; } = true;

        public long OrganizationId { get; set; }

        public string Code { get; set; }

        public string Signature { get; set; }

        public long LevelId { get; set; }

        public bool Unscored { get; set; }

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
