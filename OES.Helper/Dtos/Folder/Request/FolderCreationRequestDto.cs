namespace OES.Helper.Dtos.Folder.Request
{
    public sealed record FolderCreationRequestDto(string Name, Guid? ParentFolderId);
}
