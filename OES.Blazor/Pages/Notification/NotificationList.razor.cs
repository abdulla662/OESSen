using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Notification;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.General;

namespace OES.Blazor.Pages.Notification
{
    public partial class NotificationList
    {
        [Inject] IBlazNotificationService BlazeNotification { get; set; } = default!;

        [Inject] IDialogService DialogService { get; set; } = default!;

        public async Task<CustomTableData<CustomNotificationDto>> GetAllNotificationPaginated(PaginationSearchModel paginationSearch)
        {
            var response = await BlazeNotification.GetAllNotificationPaginated(paginationSearch);

            return response ?? new CustomTableData<CustomNotificationDto>([], 0);
        }

        private async Task OpenNotificationDialog(object item)
        {
            if (item is not long id || id <= 0) return;

            await DialogService.ShowAsync<NotificationDetailsDialog>(
                string.Empty,
                new DialogParameters<NotificationDetailsDialog>
                {
                    { x => x.NotificationId, id }
                },
                new DialogOptions
                {
                    CloseOnEscapeKey = true,
                    CloseButton = false,
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );
        }
    }
}
