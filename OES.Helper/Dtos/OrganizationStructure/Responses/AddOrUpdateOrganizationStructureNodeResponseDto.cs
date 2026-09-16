namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record AddOrUpdateOrganizationStructureNodeResponseDto(
        long Id,
        string Name,
        string Description,
        long? ParentId,
        bool IsLeaf,
        string OrganizationSignature,
        bool IsActive
    );
}
