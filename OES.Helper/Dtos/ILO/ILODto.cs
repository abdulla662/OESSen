namespace OES.Helper.Dtos.ILO
{
    public class ILODto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ParentId { get; set; }
        public string signature { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public List<Guid> GroupsIds { get; set; }
    }
}
