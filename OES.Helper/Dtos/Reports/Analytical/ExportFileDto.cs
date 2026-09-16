
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record ExportFileDto(
        string FileName,
        string ContentType,
        byte[] Bytes);
}
