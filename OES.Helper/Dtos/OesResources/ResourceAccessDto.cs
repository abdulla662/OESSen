namespace OES.Helper.Dtos.OesResources
{
    public class ResourceAccessDto
    {
        public long ResourceId { get; set; }

        public string ResourceName { get; set; }

        public List<string> Roles { get; set; } = [];

        public long? ParentId { get; set; }

        public string? ParentName { get; set; }
    }
}
