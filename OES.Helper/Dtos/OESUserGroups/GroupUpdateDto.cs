using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.OESUserGroups
{
    public class GroupUpdateDto
    {
        public Guid Id { get; set; }

        [Required (ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "GroupNameRequired")]
        [MaxLength(50, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "Groupnamecannotexceed50characters")]
        public string GroupName { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public List<Guid> RolesId { get; set; } = new List<Guid>();
    }
}
