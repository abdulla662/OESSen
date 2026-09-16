
namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record GetAllOrganizationResponseDto
    {
        public long Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public bool IsLeaf { get; set; }
    }
}