using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class SchedulePaperController(ISchedulePaperService _schedulePaperService) : OESBaseController
    {
        [HttpPost("GetAllSchedulePapersByScheduleId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllSchedulePapersByScheduleIdAsync(PaginationSearchModel paginationSearchModel, long scheduleId)
        {
            return await _schedulePaperService.GetAllSchedulePapersByScheduleIdAsync(paginationSearchModel, scheduleId);
        }

        [HttpGet("GetSchedulePaperById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePaperByIdAsync(long schedulePaperId)
        {
            return await _schedulePaperService.GetSchedulePaperByIdAsync(schedulePaperId);
        }

        [HttpGet("GetSchedulePaperSummary")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePaperSummaryAsync(long scheduleId)
        {
            return await _schedulePaperService.GetSchedulePaperSummaryAsync(scheduleId);
        }

        [HttpPost("AddSchedulePaper")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addSchedulePaperRequestDto)
        {
            return await _schedulePaperService.AddSchedulePaperAsync(addSchedulePaperRequestDto);
        }

        [HttpPut("UpdateSchedulePaper")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto updateSchedulePaperRequestDto)
        {
            return await _schedulePaperService.UpdateSchedulePaperAsync(updateSchedulePaperRequestDto);
        }

        [HttpDelete("DeleteSchedulePaper")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteSchedulePaperAsync(long schedulePaperId)
        {
            return await _schedulePaperService.DeleteSchedulePaperAsync(schedulePaperId);
        }

        [HttpGet("GetSchedulePaperAllocation")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePaperAllocation(long schedulePaperId)
        {
            return await _schedulePaperService.GetSchedulePaperAllocation(schedulePaperId);
        }

        [HttpDelete("DeletePaperAllocation")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeletePaperAllocationAsync(long venueId, long schedulePaperId)
        {
            return await _schedulePaperService.DeletePaperAllocationAsync(venueId, schedulePaperId);
        }

        [HttpGet("GetSchedulePapers")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePapersAsync(long scheduleMetadataId)
        {
            return await _schedulePaperService.GetSchedulePapersAsync(scheduleMetadataId);
        }

        [HttpPost("GetVenuesWithCandidatesCount")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            return await _schedulePaperService.GetVenuesWithCandidatesCountByPaperIdAsync(paginationSearchModel, paperId);
        }

        [HttpGet("GetSchedulePaperVenues")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePaperVenuesAsync(long schedulePaperId)
        {
            return await _schedulePaperService.GetSchedulePaperVenuesAsync(schedulePaperId);
        }

        [HttpPost("ExportCandidatesToExcel")]
        [OESFilter(Authorize = true)]
        public async Task<IActionResult> ExportCandidatesToExcelAsync(long schedulePaperId)
        {
            var fileBytes = await _schedulePaperService.ExportSchedulePaperCandidatesToExcelAsync(schedulePaperId);

            return File(fileBytes, MiscConstants.ExcelContentType, MiscConstants.CandidatesFileName);
        }

        [HttpGet("GetVenueBatches")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetVenueBatchesAsync(long venueId, long paperId)
        {
            return await _schedulePaperService.GetVenueBatchesAsync(venueId, paperId);
        }

        [HttpGet("GetSchedulePaperCandidateByRegistrationNumber")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetSchedulePaperCandidateByRegistrationNumberAsync(long registrationNumber)
        {
            return await _schedulePaperService.GetSchedulePaperCandidateByRegistrationNumberAsync(registrationNumber);
        }

        [HttpPut("UpdateSchedulePaperCandidate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateSchedulePaperCandidateAsync(UpdateSchedulePaperCandidateRequestDto request)
        {
            return await _schedulePaperService.UpdateSchedulePaperCandidateAsync(request);
        }
    }
}
