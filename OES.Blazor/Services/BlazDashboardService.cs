using OES.Blazor.Services.Interfaces;
using OES.Helper.Dtos.DashbBoard;

namespace OES.Blazor.Services
{
    public class BlazDashboardService : IBlazDashboardService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazDashboardService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            var response = await _httpClientHelper.GetAsync<DashboardStatisticsDto>("api/Dashboard/Statistics");

            return response?.Data as DashboardStatisticsDto ?? new();
        }
    }
}