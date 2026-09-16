using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ItemBankLevel
{
    public class ItemLevelsDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.LevelIsRequiredHereForItemBank))]
        public string Name { get; set; }

        public string OrganizationSignature { get; set; }
    }
}
