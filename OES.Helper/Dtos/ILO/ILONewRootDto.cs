using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ILO
{
    public class ILONewRootDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootNameIsRequired))]
        public string Name { get; set; }

        public string Description { get; set; } = "";

        [MaxLength(100)]
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootCodeRequired))]
        public string Code { get; set; }

        public List<Guid> GroupsId { get; set; } = [];
    }
}
