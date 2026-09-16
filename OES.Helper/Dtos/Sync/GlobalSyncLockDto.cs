namespace OES.Helper.Dtos.Sync
{
    public class GlobalSyncLockDto
    {
        public Guid BatchId { get; set; }

        public DateTime ExpirationTimeUtc { get; set; }
    }
}
