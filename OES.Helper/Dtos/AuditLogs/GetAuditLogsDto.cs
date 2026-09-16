namespace OES.Helper.Dtos.AuditLogs
{
    public record GetAuditLogsDto
    {
        public long Id { get; set; }

        public string Action { get; set; }

        public string CreationUser { get; set; }

        public DateTime ActionAt { get; set; }

        public string Url { get; set; }

        public string PathName { get; set; }

        public string PageName { get; set; }

        public string IPAddress { get; set; }
    }
}
