using ResourceType = OES.Helper.Enums.ResourceType;

namespace OES.Helper.Dtos.AutoPermissionAssignment.Request
{
    public class AutoPermissionAssignmentRequest
    {
        public long EntityId { get; set; }

        public string EntityName { get; set; }

        public ResourceType ResourceType { get; set; }

        public Guid? UserId { get; set; }

        public List<Guid>? AdditionalGroupIds { get; set; }

        public Type EntityGroupType { get; set; }
    }
}
