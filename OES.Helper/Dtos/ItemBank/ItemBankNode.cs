using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ItemBank
{
    public class ItemBankNode
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CodeIsRequired))]
        public string Code { get; set; }

        public string Description { get; set; } = "";

        public float Hours { get; set; } = 0;

        public string Level { get; set; }

        public long LevelId { get; set; }

        public long? ParentId { get; set; }

        public bool Unscored { get; set; }

        public bool IsActive { get; set; } = true;

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
