namespace OES.Helper.Dtos.OrganizationStructure.Responses
{
    public sealed record AddOrUpdateOrganizationStructureRootResponseDto(
        long Id,
        string Name,
        string Description,
        string OrganizationSignature,
        bool IsActive
    );
}
