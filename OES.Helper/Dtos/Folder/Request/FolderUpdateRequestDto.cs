namespace OES.Helper.Dtos.Folder.Request
{
    public sealed record FolderUpdateRequestDto(Guid FolderId,
                                                string NewName,
                                                Guid? ParentFolderId);

}
