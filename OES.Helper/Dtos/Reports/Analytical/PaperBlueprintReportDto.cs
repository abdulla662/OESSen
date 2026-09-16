
namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record PaperBlueprintReportDto(
        string Schedule,
        string PaperCode,
        List<string> ItemTypes,
        List<ItemTypeBankRowDto> Rows);
}
