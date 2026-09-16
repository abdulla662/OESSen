using OES.Helper.Dtos.QueueSuspend;
using OES.Helper.General;


namespace OES.Interface.Interfaces
{
    public interface IQueueSuspendService
    {
        Task<ApiResponse> ManagePaperSuspensionsAsync(ManagePaperSuspensionsDto requestDto, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetPaginatedPaperVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long paperId);
        Task<ApiResponse> GetSuspendedVenueCodesByPaperIdAsync(long paperId);

        Task<ApiResponse> ManageFormSuspensionsAsync(ManageFormSuspensionsDto requestDto, CancellationToken cancellationToken = default);
        Task<ApiResponse> GetPaginatedFormVenuesForSuspensionAsync(PaginationSearchModel paginationSearchModel, long formId);
        Task<ApiResponse> GetSuspendedVenueCodesByFormIdAsync(long formId);
    }
}