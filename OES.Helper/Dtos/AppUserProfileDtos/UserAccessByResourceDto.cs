using OES.Helper.Dtos.OesResources;

namespace OES.Helper.Dtos.AppUserProfileDtos
{
    public class UserAccessByResourceDto
    {
        public Guid GroupId { get; set; }

        public string GroupName { get; set; }

        public bool IsOwner { get; set; }

        public List<ResourceAccessDto> Resources { get; set; } = [];
    }
}
