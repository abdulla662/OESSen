namespace OES.Helper.Dtos.Folder.Response
{
    public sealed record OrganizationPredefinedFoldersResponseDto(
        Guid RootFolderId,
        Guid CandidatesExcelFilesFolderId
    );
}
