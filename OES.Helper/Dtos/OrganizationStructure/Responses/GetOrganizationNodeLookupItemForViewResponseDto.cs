namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record GetOrganizationNodeLookupItemForViewResponseDto(
        long LookupItemId,
        string LookupItemName,
        long OrganizationStructureNodeId,
        string OrganizationStructureNodeName
    );
}
