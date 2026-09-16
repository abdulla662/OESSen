using OES.Helper.General;

namespace OES.Helper.Dtos.Sync
{
    public class AuditContextDto
    {
        public string User { get; set; }
        public long OrganizationId { get; set; }
        public string OrganizationSignature { get; set; }

        // Used by HTTP-triggered calls
        public static AuditContextDto FromHttpContext(FilterParamsValues filter) => new()
        {
            User = filter.UserEmail,
            OrganizationId = filter.OrganizationId,
            OrganizationSignature = filter.Signature
        };

        // Used by background jobs — built from the schedule's own BaseEntity fields
        public static AuditContextDto FromSchedule(long orgId, string orgSignature) => new()
        {
            User = "auto-sync-job",
            OrganizationId = orgId,
            OrganizationSignature = orgSignature
        };
    }
}
