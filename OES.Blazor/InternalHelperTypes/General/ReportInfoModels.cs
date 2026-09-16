namespace OES.Blazor.InternalHelperTypes.General
{
    public record ReportInfoSection(string Heading, List<ReportInfoItem> Items);

    public record ReportInfoItem(string Label, string Description);
}
