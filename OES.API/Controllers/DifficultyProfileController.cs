using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class DifficultyProfileController(IDifficultyProfileService _difficultyProfile) : OESBaseController
    {
        [HttpPost("GetAllDifficultyProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllDifficultyProfile(PaginationSearchModel searchModel)
        {
            return await _difficultyProfile.GetAllDifficultyProfile(searchModel);
        }

        [HttpGet("GetProfiles")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetProfiles()
        {
            return await _difficultyProfile.GetProfiles();
        }

        [HttpGet("GetProfileById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetProfileById(long id)
        {
            return await _difficultyProfile.GetProfileById(id);
        }

        [HttpPost("AddDifficultyProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddDifficultyProfile(AddDifficultyProfileDto _addDifficultyProfile)
        {
            return await _difficultyProfile.AddDifficultyProfile(_addDifficultyProfile);
        }

        [HttpPut("UpdateDifficultyProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateDifficultyProfile(DifficultyProfileDto _updateDifficultyProfile)
        {
            return await _difficultyProfile.UpdateDifficultyProfile(_updateDifficultyProfile);
        }

        [HttpDelete("SoftDeleteDifficultyProfile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> SoftDeleteDifficultyProfile(long id)
        {
            return await _difficultyProfile.SoftDeleteDifficultyProfileAsync(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserDifficultyProfileGroupsAsync")]
        //public async Task<ApiResponse> GetUserDifficultyProfileGroupsAsync()
        //{
        //    return await _difficultyProfile.GetUserDifficultyProfileGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetDifficultyProfileGroupsAsync")]
        //public async Task<IApiResponse> GetDifficultyProfileGroupsAsync(long difficultyProfileId)
        //{
        //    return await _difficultyProfile.GetDifficultyProfileGroupsAsync(difficultyProfileId);
        //}
    }
}
