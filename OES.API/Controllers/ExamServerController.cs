using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ExamServerController(IExamServerService _examServer) : OESBaseController
    {
        [OESFilter(Authorize = true)]
        [HttpPost("BulkSync")]
        public async Task<ApiResponse> GetAllScheduleAsync(long scheduleId, [FromBody] List<long> venueIds)
        {
            return await _examServer.BulkSync(scheduleId, venueIds: venueIds);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("ValidateScheduleForSync")]
        public async Task<ApiResponse> ValidateScheduleForSyncAsync(long scheduleId)
        {
            return await _examServer.ValidateScheduleForSyncAsync(scheduleId);
        }
    }
}