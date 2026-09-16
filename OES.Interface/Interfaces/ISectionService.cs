using OES.Helper.Dtos.Section;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface ISectionService
    {
        Task<ApiResponse> EditSectionAsync(EditSectionDto sectionDto);
    }
}
