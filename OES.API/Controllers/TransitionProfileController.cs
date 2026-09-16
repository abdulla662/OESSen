using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class TransitionProfileController(ITransitionProfileService _profileService) : OESBaseController
    {
        [HttpPost("AddTransitionProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddTransitionProfile(AddTransitionProfileDto _addProfile)
        {
            return await _profileService.AddTransitionProfile(_addProfile);
        }


        [HttpPost("GetAllTransitionProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllTransitionProfile(PaginationSearchModel searchModel)
        {
            return await _profileService.GetAllTransitionProfile(searchModel);
        }


        [HttpPut("UpdateTransitionProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> EditTransitionProfileAsync(TransitionProfileUpdateRequestDto transitionProfileUpdateRequestDto)
        {
            return await _profileService.EditTransitionProfileAsync(transitionProfileUpdateRequestDto);
        }


        [HttpGet("GetAllProfiles")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllProfiles()
        {
            return await _profileService.GetProfiles();
        }


        [HttpDelete("DeleteTransitionProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> SoftDeleteTransitionProfileAsync(long profileId)
        {
            return await _profileService.SoftDeleteTransitionProfileAsync(profileId);
        }


        [HttpGet("GetTransitionProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetTransitionProfileById(long profileId)
        {
            return await _profileService.GetTransitionProfileById(profileId);
        }


        //[OESFilter(Authorize = true)]
        //[HttpGet("GetTransitionProfileGroupsAsync")]
        //public async Task<ApiResponse> GetTransitionProfileGroupsAsync()
        //{
        //    return await _profileService.GetTransitionProfileGroupsAsync();
        //}


        //[OESFilter(Authorize = true)]
        //[HttpGet("GetTransitionGroupsAsync")]
        //public async Task<IApiResponse> GetTransitionGroupsAsync(long TransitionProfileId)
        //{
        //    return await _profileService.GetTransitionGroupsAsync(TransitionProfileId);
        //}
    }
}
