using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Notification
{
    public class BlazNotificationService : IBlazNotificationService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        private readonly IBlazGetCustomTableData<CustomNotificationDto> _blazGetCustomTableData;


        public BlazNotificationService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<CustomNotificationDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;

            _blazGetCustomTableData = blazGetCustomTableData;
        }


        public Task<ApiResponse> CreateNewNotification(IEnumerable<CreateNewNotificationDto> createNewNotificationDtos)
        {
            throw new NotImplementedException();
        }

        public async Task<CustomTableData<CustomNotificationDto>> GetAllNotificationPaginated(PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableData.GetCustomTableData(paginationSearch, "api/Notification/getAllNotificationAsync");

            return result;
        }

        public async Task<List<NotificationAppUserProfileDto>> GetFilteredNotification(Guid id)
        {
            var response = await _httpClientHelper.GetAsync<List<NotificationAppUserProfileDto>>("api/Notification/GetAllFilteredNotification?id=" + id);

            if (response != null)
            {
                return response.Data as List<NotificationAppUserProfileDto>;
            }

            return [];
        }

        public async Task<ApiResponse> GetNotificationById(long id)
        {
            var response = await _httpClientHelper.GetAsync<NotificationAppUserProfileDto>($"api/Notification/getNotificationById?id={id}");

            return response;
        }
    }
}
