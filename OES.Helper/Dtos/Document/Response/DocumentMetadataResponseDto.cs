namespace OES.Helper.Dtos.Document.Response
{
    public sealed record DocumentMetadataResponseDto(Guid Id,
                                                     string Name,
                                                     string Type,
                                                     long Size);
}
