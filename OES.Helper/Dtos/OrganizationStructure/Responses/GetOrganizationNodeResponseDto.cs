using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record GetOrganizationNodeResponseDto
    {
        public long Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public bool IsFirstLevelChildOfRootNode { get; init; }

        public List<GetOrganizationNodeLookupItemResponseDto> LookupItems { get; set; }

        [JsonConstructor]
        public GetOrganizationNodeResponseDto(long id,
                                              string name,
                                              string description,
                                              bool isFirstLevelChildOfRootNode,
                                              List<GetOrganizationNodeLookupItemResponseDto> lookupItems)
        {
            Id = id;
            Name = name;
            Description = description;
            IsFirstLevelChildOfRootNode = isFirstLevelChildOfRootNode;
            LookupItems = lookupItems;
        }
    }
}
