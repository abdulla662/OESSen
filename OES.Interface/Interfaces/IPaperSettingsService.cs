using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IPaperSettingsService
    {
        Task<ApiResponse> GetAllPaperSettingsTemplatesPaginatedAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetPaperSettingsTemplateByIdAsync(long id);

        Task<ApiResponse> GetPaperSettingsBySchedulePaperIdAsync(long schedulePaperId);

        Task<ApiResponse> AddPaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto addPaperSettingsRequestDto);

        Task<ApiResponse> UpdatePaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto updatePaperSettingsRequestDto);

        Task<ApiResponse> AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto addPaperSettingsTemplateRequestDto);

        Task<ApiResponse> UpdatePaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto updatePaperSettingsTemplateRequestDto);

        Task<ApiResponse> DeletePaperSettingsTemplateAsync(long id);
    }
}
