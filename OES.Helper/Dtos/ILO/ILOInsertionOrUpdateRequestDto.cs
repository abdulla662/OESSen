namespace OES.Helper.Dtos.ILO
{
    public sealed record ILOInsertionOrUpdateRequestDto(
        long Id,
        string Name,
        string Description,
        long? ParentId,
        string Code,
        bool IsActive,
        long OrganizationId,
        List<Guid>? GroupsIds
    );
}
