using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces
{
    public interface IBlazQueueSuspendService
    {
        Task<ApiResponse> ManagePaperSuspensionsAsync(ManagePaperSuspensionsDto requestDto);
        Task<CustomTableData<QueueSuspendResponseDto>> GetPaginatedPaperVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long paperId);
        Task<List<string>> GetSuspendedVenueCodesByPaperIdAsync(long paperId);

        Task<ApiResponse> ManageFormSuspensionsAsync(ManageFormSuspensionsDto requestDto);
        Task<CustomTableData<QueueSuspendResponseDto>> GetPaginatedFormVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long formId);
        Task<List<string>> GetSuspendedVenueCodesByFormIdAsync(long formId);
    }
}