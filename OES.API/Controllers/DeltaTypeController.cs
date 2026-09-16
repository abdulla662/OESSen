using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class DeltaTypeController(IDeltaTypeService _deltaType) : OESBaseController
    {
        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        [HttpGet("GetDeltaTypes")]
        public async Task<IApiResponse> GetDeltaTypes()
        {
            return await _deltaType.GetAllDeltaTypes();
        }

        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        [HttpPost("GetDeltaTypesPaginated")]
        public async Task<IApiResponse> GetDeltaTypesWithPagination(PaginationSearchModel pagination)
        {
            return await _deltaType.GetAllDeltaType(pagination);
        }

        [OESFilter(Authorize = true, ApplySignatureFilter = false, ApplyOrganizationIdFilter = false)]
        [HttpGet("GetDeltaTypeById/{id}")]
        public async Task<IApiResponse> GetDeltaTypeById(long id)
        {
            return await _deltaType.GetDeltaTypeById(id);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AddDeltaType")]
        public async Task<IApiResponse> AddDeltaType(AddDeltaTypeDto addDeltaTypeDto)
        {
            return await _deltaType.AddDeltaType(addDeltaTypeDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPut("UpdateDeltaType")]
        public async Task<IApiResponse> UpdateDeltaType(GetDeltaTypeDto updateDeltaTypeDto)
        {
            return await _deltaType.UpdateDeltaType(updateDeltaTypeDto);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("SoftDeleteDeltaType/{id}")]
        public async Task<IApiResponse> SoftDeleteDeltaType(long id)
        {
            return await _deltaType.SoftDeleteDeltaType(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserDeltaTypeGroupsAsync")]
        //public async Task<ApiResponse> GetUserDeltaTypeGroupsAsync()
        //{
        //    return await _deltaType.GetUserDeltaTypeGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetDeltaTypeGroupsAsync")]
        //public async Task<IApiResponse> GetDeltaTypeGroupsAsync(long deltaTypeId)
        //{
        //    return await _deltaType.GetDeltaTypeGroupsAsync(deltaTypeId);
        //} 
    }
}
