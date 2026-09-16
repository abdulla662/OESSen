using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.MarkingScheme
{
    public interface IBlazMarkingSchemeService
    {
        Task<MarkingSchemeDto> GetPaperMarkingSchemeAsync(long paperId);

        Task<ApiResponse> AddOrUpdateMarkingSchemeAsync(MarkingSchemeDto markingSchemeDto);
    }
}