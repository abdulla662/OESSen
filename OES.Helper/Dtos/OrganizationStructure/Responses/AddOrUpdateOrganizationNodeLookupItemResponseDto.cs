namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record AddOrUpdateOrganizationNodeLookupItemResponseDto(
        long Id,
        string Name,
        long? ParentLookupItemId,
        long OrganizationStructureNodeId
    );
}
