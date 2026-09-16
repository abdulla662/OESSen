namespace OES.Helper.Dtos.Document.Response
{
    public sealed record DocumentDownloadResponseDto(byte[] FileContents,
                                                     string ContentType,
                                                     string FileDownloadName);

}
