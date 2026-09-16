namespace OES.Helper.Dtos.User
{
    public class OrganizationTreeNodeDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public long? ParentId { get; set; }

        public bool IsExpanded { get; set; } = true;

        public List<OrganizationTreeNodeDto> Children { get; set; } = [];
    }
}
