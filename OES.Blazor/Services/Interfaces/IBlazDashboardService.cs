using OES.Helper.Dtos.DashbBoard;

namespace OES.Blazor.Services.Interfaces
{
    public interface IBlazDashboardService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
    }
}
