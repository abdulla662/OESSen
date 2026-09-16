using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class DisabilityController : OESBaseController
    {
        private readonly IDisabilityService _disabilityService;

        public DisabilityController(IDisabilityService disabilityService)
        {
            _disabilityService = disabilityService;
        }

        [HttpPost("GetAllDisabilities")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllDisabilitiesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _disabilityService.GetAllDisabilitiesForPaginationAsync(paginationSearchModel);
        }

        [HttpGet("GetDisabilityById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetDisabilityByIdAsync(long id)
        {
            return await _disabilityService.GetDisabilityByIdAsync(id);
        }
        [HttpGet("GetAllDisabilities")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllDisabilitiesAsync()
        {
            return await _disabilityService.GetAllDisabilitiesAsync();
        }

        [HttpPost("AddDisability")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisabilityDto)
        {
            return await _disabilityService.AddDisabilityAsync(addOrUpdateDisabilityDto);
        }

        [HttpDelete("DeleteDisability")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteDisabilityAsync(long id)
        {
            return await _disabilityService.DeleteDisabilityAsync(id);
        }

        [HttpPut("UpdateDisability")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisability)
        {
            return await _disabilityService.UpdateDisabilityAsync(addOrUpdateDisability);
        }

        [HttpPost("AddCandidateExtraTime")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto addCandidateExtraTimeDto)
        {
            return await _disabilityService.AddCandidateExtraTimeAsync(addCandidateExtraTimeDto);
        }
    }
}
