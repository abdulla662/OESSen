using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IJobManagementService
    {
        Task<ApiResponse> RetryFailedJobs(List<long> jobIds, CancellationToken cancellationToken = default);

        Task<ApiResponse> GetJobsByBatchId(Guid batchId);

        Task<ApiResponse> GetSyncHistory(DateTime fromDate, CancellationToken cancellationToken = default);

        Task<ApiResponse> CancelPendingJob(long jobId, CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType, string FileName)> GetPayloadStreamAsync(string fileName);
    }
}