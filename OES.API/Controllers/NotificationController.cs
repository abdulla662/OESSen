using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class NotificationController : OESBaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [OESFilter(Authorize = true)]
        [HttpPost("getAllNotificationAsync")]
        public async Task<ApiResponse> getAllNotificationAsync([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _notificationService.GetAllNotificationPaginated(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("getNotificationById")]
        public async Task<ApiResponse> getNotificationById([FromQuery] long id)
        {
            return await _notificationService.GetNotificationById(id);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllFilteredNotification")]
        public async Task<ApiResponse> GetAllFilteredNotification([FromQuery] Guid id)
        {
            return await _notificationService.GetFilteredNotification(id);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("CreateNewNotification")]
        public async Task<ApiResponse> CreateNewNotification(CreateNewNotificationDto createNewNotificationDtos)
        {
            return await _notificationService.CreateNewNotification(createNewNotificationDtos);
        }
    }
}