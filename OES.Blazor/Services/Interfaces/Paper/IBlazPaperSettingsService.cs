using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.General;
namespace OES.Blazor.Services.Interfaces.Paper
{
    public interface IBlazPaperSettingsService
    {
        Task<CustomTableData<PaperSettingsTemplatePaginated>> GetAllPaperSettingsTemplatesPaginatedAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetPaperSettingsTemplateByIdAsync(long id);

        Task<GetPaperSettingsResponseDto> GetPaperSettingsBySchedulePaperIdAsync(long schedulePaperId);

        Task<ApiResponse> AddPaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto addPaperSettingsRequestDto);

        Task<ApiResponse> UpdatePaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto updatePaperSettingsRequestDto);

        Task<ApiResponse> AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto addPaperSettingsTemplateRequestDto);

        Task<ApiResponse> UpdatePaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto updatePaperSettingsTemplateRequestDto);

        Task<ApiResponse> DeletePaperSettingsTemplateAsync(long id);
    }
}