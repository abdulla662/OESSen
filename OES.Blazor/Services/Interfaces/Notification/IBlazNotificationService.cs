using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Notification
{
    public interface IBlazNotificationService
    {
        Task<CustomTableData<CustomNotificationDto>> GetAllNotificationPaginated(PaginationSearchModel paginationSearch);
        Task<ApiResponse> GetNotificationById(long id);
        Task<List<NotificationAppUserProfileDto>> GetFilteredNotification(Guid id);
        Task<ApiResponse> CreateNewNotification(IEnumerable<CreateNewNotificationDto> createNewNotificationDtos);
    }
}
