namespace OES.Helper.Dtos.OESUserGroups
{
    public class GetOESGroupDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool AutoCreatedForUser { get; set; }
    }
}
