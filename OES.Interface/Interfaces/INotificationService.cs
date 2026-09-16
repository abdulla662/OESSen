using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse> GetAllNotificationPaginated(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetNotificationById(long id);
        Task<ApiResponse> GetFilteredNotification(Guid id);
        Task<ApiResponse> CreateNewNotification(CreateNewNotificationDto createNewNotificationDtos);
        Task SendNotificationForNewItemBank(List<Guid> groupIds, CreateNewNotificationDto createNewNotification);
    }
}
