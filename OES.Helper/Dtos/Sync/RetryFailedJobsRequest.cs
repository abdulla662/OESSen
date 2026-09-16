namespace OES.Helper.Dtos.Sync
{
    public record RetryFailedJobsRequest(List<long> JobIds);
}