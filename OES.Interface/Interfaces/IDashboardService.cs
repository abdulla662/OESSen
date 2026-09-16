using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
