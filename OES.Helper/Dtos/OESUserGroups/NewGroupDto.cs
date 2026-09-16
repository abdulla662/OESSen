using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.OESUserGroups
{
    public class NewGroupDto
    {
        [Required (ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "GroupNameRequired")]
        public string GroupName { get; set; }

        public string? Description { get; set; } = null;

        public List<Guid> RolesId { get; set; } = new List<Guid>();
    }
}
