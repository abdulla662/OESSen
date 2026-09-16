using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ILO
{
    public class IloEditDTO
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootNameIsRequired))]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootCodeRequired))]
        [MaxLength(100)]
        public string Code { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; } = "";

        public List<Guid> GroupsId { get; set; }
    }
}
