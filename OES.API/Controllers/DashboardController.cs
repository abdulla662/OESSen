using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class DashboardController : OESBaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [OESFilter(Authorize = true)]
        [HttpGet("Statistics")]
        public async Task<IApiResponse> Statistics()
        {
            return await _dashboardService.GetDashboardStatisticsAsync();
        }
    }
}