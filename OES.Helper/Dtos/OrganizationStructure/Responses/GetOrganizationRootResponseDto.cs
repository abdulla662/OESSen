namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public class GetOrganizationRootResponseDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool ConsiderVenues { get; set; }

        public GetOrganizationRootResponseDto() { }

        public GetOrganizationRootResponseDto(long id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}