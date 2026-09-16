using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record GetOrganizationNodeLookupItemResponseDto
    {
        public long Id { get; init; }

        public string Name { get; set; }

        public long? ParentLookupItemId { get; set; }

        public long OrganizationStructureNodeId { get; set; }


        [JsonConstructor]
        public GetOrganizationNodeLookupItemResponseDto(long id,
                                                        string name,
                                                        long? parentLookupItemId,
                                                        long organizationStructureNodeId)
        {
            Id = id;
            Name = name;
            ParentLookupItemId = parentLookupItemId;
            OrganizationStructureNodeId = organizationStructureNodeId;
        }
    }
}
