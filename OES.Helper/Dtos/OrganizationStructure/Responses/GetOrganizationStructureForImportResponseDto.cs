namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record GetOrganizationStructureForImportResponseDto(long LookupItemId,
                                                                      string LookupItemName,
                                                                      string OrganizationNodeName,
                                                                      long? ParentLookupItemId,
                                                                      long OrganizationStructureNodeId,
                                                                      bool IsRoot);
}