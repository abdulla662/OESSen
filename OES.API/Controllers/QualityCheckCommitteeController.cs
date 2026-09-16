using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QualityCheckCommitteeController : OESBaseController, IQualityCheckCommitteeService
    {
        private readonly IQualityCheckCommitteeService _qualityCheckCommitteeService;

        public QualityCheckCommitteeController(IQualityCheckCommitteeService qualityCheckCommitteeService)
        {
            _qualityCheckCommitteeService = qualityCheckCommitteeService;
        }

        [OESFilter(Authorize = true)]
        [HttpPost("createCommitteeWithMembers")]
        public async Task<ApiResponse> CreateCommitteeWithMembersAsync(CreateQualityCheckCommitteeRequestDto dto)
        {
            return await _qualityCheckCommitteeService.CreateCommitteeWithMembersAsync(dto);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("getAllCommitteesPaginated")]
        public async Task<ApiResponse> GetAllQualityCheckCommitteePaginatedListAsync(PaginationSearchModel pagination)
        {
            return await _qualityCheckCommitteeService.GetAllQualityCheckCommitteePaginatedListAsync(pagination);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("getAllCommittees")]
        public async Task<ApiResponse> GetAllQualityCheckCommitteeAsync()
        {
            return await _qualityCheckCommitteeService.GetAllQualityCheckCommitteeAsync();
        }

        [OESFilter(Authorize = true)]
        [HttpGet("getCommitteeById")]
        public async Task<ApiResponse> GetQualityCheckCommitteeByIdAsync(long id)
        {
            return await _qualityCheckCommitteeService.GetQualityCheckCommitteeByIdAsync(id);
        }

        [OESFilter(Authorize = true, ApplyIsActiveFilter = false)]
        [HttpGet("getCommitteeMembers")]
        public async Task<ApiResponse> GetQualityCheckCommitteeMembersAsync(long committeeId)
        {
            return await _qualityCheckCommitteeService.GetQualityCheckCommitteeMembersAsync(committeeId);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("editCommitteeAsync")]
        public async Task<ApiResponse> EditQualityCheckCommitteeByIdAsync(QualityCheckCommitteeDto dto)
        {
            return await _qualityCheckCommitteeService.EditQualityCheckCommitteeByIdAsync(dto);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserQualityCheckCommitteeGroupsAsync")]
        //public async Task<ApiResponse> GetUserQualityCheckCommitteeGroupsAsync()
        //{ 
        //    return await _qualityCheckCommitteeService.GetUserQualityCheckCommitteeGroupsAsync(); }
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetQualityCheckCommitteeGroupsAsync")]
        //public async Task<ApiResponse> GetQualityCheckCommitteeGroupsAsync(long qualityCheckCommitteeId)
        //{ 
        //    return await _qualityCheckCommitteeService.GetQualityCheckCommitteeGroupsAsync(qualityCheckCommitteeId);
        //}
    }
}
