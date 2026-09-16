using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IMarkingSchemeService
    {
        Task<ApiResponse> GetPaperMarkingSchemeAsync(long paperId);

        Task<ApiResponse> AddOrUpdateMarkingSchemeAsync(MarkingSchemeDto markingSchemeDto);

        Task<ApiResponse> DeletePaperMarkingSchemeAsync(long paperId);
    }
}
