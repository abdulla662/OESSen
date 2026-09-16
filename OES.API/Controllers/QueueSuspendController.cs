using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QueueSuspendController : OESBaseController
    {
        private readonly IQueueSuspendService _queueSuspendService;

        public QueueSuspendController(IQueueSuspendService queueSuspendService)
        {
            _queueSuspendService = queueSuspendService;
        }

        [HttpPost("ManagePaperSuspensions")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ManagePaperSuspensionsAsync([FromBody] ManagePaperSuspensionsDto request, CancellationToken cancellationToken)
        {
            return await _queueSuspendService.ManagePaperSuspensionsAsync(request, cancellationToken);
        }

        [HttpPost("GetPaginatedPaperVenuesForSuspension")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedPaperVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            return await _queueSuspendService.GetPaginatedPaperVenuesForSuspensionAsync(paginationSearchModel, paperId);
        }

        [HttpGet("GetSuspendedVenueCodesByPaperId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSuspendedVenueCodesByPaperIdAsync(long paperId)
        {
            return await _queueSuspendService.GetSuspendedVenueCodesByPaperIdAsync(paperId);
        }

        [HttpPost("ManageFormSuspensions")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> ManageFormSuspensionsAsync([FromBody] ManageFormSuspensionsDto request, CancellationToken cancellationToken)
        {
            return await _queueSuspendService.ManageFormSuspensionsAsync(request, cancellationToken);
        }

        [HttpPost("GetPaginatedFormVenuesForSuspension")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetPaginatedFormVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long formId)
        {
            return await _queueSuspendService.GetPaginatedFormVenuesForSuspensionAsync(paginationSearchModel, formId);
        }

        [HttpGet("GetSuspendedVenueCodesByFormId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSuspendedVenueCodesByFormIdAsync(long formId)
        {
            return await _queueSuspendService.GetSuspendedVenueCodesByFormIdAsync(formId);
        }
    }
}