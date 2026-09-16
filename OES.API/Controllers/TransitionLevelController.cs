using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class TransitionLevelController(ITransitionLevelService _transitionLevel) : OESBaseController
    {
        [HttpPost("GetAllTransitionLevel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllTransitionLevel(PaginationSearchModel searchModel)
        {
            return await _transitionLevel.GetAllTransitionLevel(searchModel);
        }


        [HttpGet("GetTransitionLevels")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetTransitionLevels()
        {
            return await _transitionLevel.GetTransitionLevels();
        }


        [HttpGet("GetTransitionLevelsByProfileId")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetTransitionLevelsByProfileId(long profileId)
        {
            return await _transitionLevel.GetTransitionLevelsByProfileId(profileId);
        }


        [HttpGet("GetTransitionLevelById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetTransitionLevelById(long id)
        {
            return await _transitionLevel.GetTransitionLevelById(id);
        }


        [HttpPost("AddTransitionLevel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddTransitionLevel(AddTransitionLevelRequestDto _addTransition)
        {
            return await _transitionLevel.AddTransitionLevel(_addTransition);
        }


        [HttpPut("UpdateTransitionLevel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateTransitionLevel(GetTransitionLevelDto transitionLevelDto)
        {
            return await _transitionLevel.UpdateTransitionLevel(transitionLevelDto);
        }


        [HttpDelete("SoftDeleteTransitionLevelAsync")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> SoftDeleteTransitionLevelAsync(long id)
        {
            return await _transitionLevel.SoftDeleteTransitionLevelAsync(id);
        }
    }
}
