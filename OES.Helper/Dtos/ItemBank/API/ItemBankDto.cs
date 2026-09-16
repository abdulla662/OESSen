using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ItemBank.API
{
    public class ItemBankDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootNameIsRequired))]
        [MaxLength(120)]
        public string Name { get; set; }

        public string Code { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootDescriptionIsRequired))]
        [MaxLength(250)]
        public string Description { get; set; } = "";

        public float Hours { get; set; } = 0;

        public long LevelId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.LevelIsRequiredHereForItemBank))]
        public string LevelName { get; set; }

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
