using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.AppUserProfileDtos
{
    public class UsersToGroupDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public Guid GroupId { get; set; }

        public List<Guid> UsersIds { get; set; } = [];
    }
}
