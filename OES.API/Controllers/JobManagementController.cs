using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class JobManagementController(IJobManagementService jobManagementService) : OESBaseController
    {
        [OESFilter(Authorize = true)]
        [HttpPost("RetryFailedJobs")]
        public async Task<ApiResponse> RetryFailedJobs(RetryFailedJobsRequest request)
        {
            return await jobManagementService.RetryFailedJobs(request.JobIds);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetJobsByBatchId")]
        public async Task<ApiResponse> GetJobsByBatchId([FromQuery] Guid batchId)
        {
            return await jobManagementService.GetJobsByBatchId(batchId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetSyncHistory")]
        public async Task<ApiResponse> GetSyncHistory([FromQuery] DateTime fromDate)
        {
            return await jobManagementService.GetSyncHistory(fromDate);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("CancelPendingJob/{jobId}")]
        public async Task<ApiResponse> CancelPendingJob(long jobId)
        {
            return await jobManagementService.CancelPendingJob(jobId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("DownloadPayload")]
        [DoNotEncrypt]
        public async Task<IActionResult> DownloadPayload([FromQuery] string fileName)
        {
            var (stream, contentType, downloadFileName) = await jobManagementService.GetPayloadStreamAsync(fileName);
            return File(stream, contentType, downloadFileName);
        }
    }
}