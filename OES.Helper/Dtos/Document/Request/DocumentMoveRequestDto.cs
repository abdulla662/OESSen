namespace OES.Helper.Dtos.Document.Request
{
    public sealed record DocumentMoveRequestDto(Guid DocumentId,
                                               Guid NewFolderId);
}
