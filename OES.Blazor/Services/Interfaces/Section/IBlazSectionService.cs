using OES.Helper.Dtos.Section;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Section
{
    public interface IBlazSectionService
    {
        Task<ApiResponse> EditSectionAsync(EditSectionDto sectionDto);
    }
}
