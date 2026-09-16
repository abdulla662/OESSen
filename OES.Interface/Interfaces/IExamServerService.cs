using OES.Helper.Dtos.Sync;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IExamServerService
    {
        Task<ApiResponse> BulkSync(
            long scheduleId,
            long? paperId = null,
            long? formId = null,
            bool isPartialSync = false,
            bool isAutoSync = false,
            List<long> venueIds = null,
            List<long> candidateIdsParam = null,
            AuditContextDto audit = null,
            CancellationToken cancellationToken = default);

        Task<ApiResponse> ValidateScheduleForSyncAsync(long scheduleId, CancellationToken cancellationToken = default);
    }
}