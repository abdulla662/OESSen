using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class LanguageController(ILanguageService _languageService) : OESBaseController
    {
        [OESFilter(Authorize = true)]
        [HttpGet("GetLanguageById")]
        public async Task<ApiResponse> GetLanguageById(long id)
        {
            return await _languageService.GetLanguageByIdAsync(id);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllLanguagesList")]
        public async Task<ApiResponse> GetAllLanguagesList()
        {
            return await _languageService.GetAllLanguagesAsync();
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllQuestionDetailsLanguagesGroup")]
        public async Task<ApiResponse> GetAllQuestionDetailsLanguagesGroupAsync(long questionMetadataId)
        {
            return await _languageService.GetAllQuestionDetailsLanguagesGroupAsync(questionMetadataId);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetPaginatedLanguagesList")]
        public async Task<ApiResponse> GetPaginatedLanguagesList(PaginationSearchModel pagination)
        {
            return await _languageService.GetAllPaginatedLanguagesAsync(pagination);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AddLanguage")]
        public async Task<ApiResponse> AddLanguageAsync(LanguageCreateDto languageCreateDto)
        {
            return await _languageService.AddLanguageAsync(languageCreateDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPut("UpdateLanguage")]
        public async Task<ApiResponse> UpdateLanguage([FromBody] LanguageUpdateDto languageUpdateDto)
        {
            return await _languageService.UpdateLanguageAsync(languageUpdateDto);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteLanguage")]
        public async Task<ApiResponse> SoftDeleteLanguageAsync(long id)
        {
            return await _languageService.SoftDeleteLanguageAsync(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserLanguageGroupsAsync")]
        //public async Task<ApiResponse> GetUserLanguageGroupsAsync()
        //{
        //    return await _languageService.GetUserLanguageGroupsAsync(); 
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetLanguageGroupsAsync")]
        //public async Task<IApiResponse> GetLanguageGroupsAsync(long languageId)
        //{
        //    return await _languageService.GetLanguageGroupsAsync(languageId);
        //}
    }
}