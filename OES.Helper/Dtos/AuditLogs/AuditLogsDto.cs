namespace OES.Helper.Dtos.AuditLogs
{
    public record AuditLogsDto
    {
        public string Action { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public DateTime ActionAt { get; set; }

        public string PathName { get; set; } = string.Empty;

        public string PageName { get; set; } = string.Empty;
    }
}
