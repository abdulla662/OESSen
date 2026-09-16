namespace OES.Helper.Dtos.User
{
    public class OrganizationSelectionListDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public long? ParentId { get; set; }
    }
}
