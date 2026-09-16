using OES.Helper.Dtos.OESUserGroups;
using System.ComponentModel;

namespace OES.Helper.Dtos.ItemBank.API
{
    public class NewItemBankDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public long? ParentId { get; set; }
        public string ItemBankSignature { get; set; }
        public long OrganizationId { get; set; }
        public string OrganizationSignature { get; set; }
        public float Hours { get; set; } = 0;
        public int TotalQuestions { get; set; }
        public bool IsActive { get; set; } = true;
        public long? LevelId { get; set; }
        public string? LevelName { get; set; } = string.Empty;
        public bool Unscored { get; set; }
        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
