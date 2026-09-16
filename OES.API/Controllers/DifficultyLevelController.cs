using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class DifficultyLevelController(IDifficultyLevelService _difficultyLevelService) : OESBaseController
    {
        [HttpGet("GetAllDifficultyLevels")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllDifficultyLevels()
        {
            return await _difficultyLevelService.GetAllDifficultyLevels();
        }

        [HttpGet("GetDifficultyLevelByProfileId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetAllDifficultyLevelsByProfileIdAsync(long? profileId)
        {
            return await _difficultyLevelService.GetAllDifficultyLevelsByProfileIdAsync(profileId);
        }

        [HttpGet("GetDifficultyLevelWithQuestionCountByProfileId")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetDifficultyLevelWithQuestionCountByProfileIdAsync(long? profileId)
        {
            return await _difficultyLevelService.GetDifficultyLevelWithQuestionCountByProfileIdAsync(profileId);
        }

        [HttpPost("PaginatedDifficultyLevel")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaginatedDifficultyLevels(PaginationSearchModel pagination)
        {
            return await _difficultyLevelService.GetAllDifficultyLevel(pagination);
        }

        [HttpPost("AddDifficultyLevelAsync")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> AddDifficultyLevelAsync(AddDifficultyLevelDto levelDto)
        {
            return await _difficultyLevelService.AddDifficultyLevelAsync(levelDto);
        }

        [HttpGet("GetDifficultyLevelById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetDifficultyLevelById(long id)
        {
            return await _difficultyLevelService.GetDifficultyLevelByIdAsync(id);
        }

        [HttpGet("GetDifficultyLevelsByDeltaTypeId")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetDifficultyLevelsByDeltaTypeId(long DeltaTypeId)
        {
            return await _difficultyLevelService.GetDifficultyLevelsByDeltaTypeId(DeltaTypeId);
        }

        [HttpPut("UpdateDifficultyLevel")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> UpdateDifficultyLevel([FromBody] UpdateDifficultyLevelDto updateDto)
        {
            return await _difficultyLevelService.UpdateDifficultyLevelAsync(updateDto);
        }

        [HttpPost("DeleteDifficultyLevel")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeleteDifficultyLevel([FromBody] long id)
        {
            return await _difficultyLevelService.DeleteDifficultyLevelAsync(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserDifficultyLevelGroupsAsync")]
        //public async Task<ApiResponse> GetUserDifficultyLevelGroupsAsync()
        //{ 
        //    return await _difficultyLevelService.GetUserDifficultyLevelGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetDifficultyLevelGroupsAsync")]
        //public async Task<IApiResponse> GetDifficultyLevelGroupsAsync(long difficultyLevelId)
        //{ 
        //    return await _difficultyLevelService.GetDifficultyLevelGroupsAsync(difficultyLevelId);
        //}
    }
}
