namespace OES.Helper.Dtos.Document.Request
{
    public sealed record DocumentUpdateRequestDto(Guid DocumentId,
                                                  string NewName,
                                                  Guid FolderId);
}
