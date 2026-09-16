using OES.Helper.Dtos.ExamServer;

namespace OES.Helper.Dtos.Sync
{
    public class SyncPayloadMessage
    {
        public Guid JobId { get; set; }
        public string VenueCode { get; set; }
        public bool IsPartialSync { get; set; }
        public VenueSyncPayload VenueSyncPayload { get; set; } // Don't change this property name case, keep it as is.
    }
}