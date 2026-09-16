using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.Notification
{
    public partial class NotificationDetailsDialog : ComponentBase
    {
        [Inject] IBlazNotificationService BlazeNotification { get; set; } = default!;
        [Inject] private IBlazSessionStorageService SessionStorage { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long NotificationId { get; set; }

        private NotificationAppUserProfileDto NotificationItem { get; set; } = null;

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadNotification();
        }

        private async Task LoadNotification()
        {
            _loading = true;
            StateHasChanged();

            var id = NotificationId > 0
                ? NotificationId
                : await SessionStorage.GetValue<long>(MiscConstants.PerformViewBtnClick);

            var response = await BlazeNotification.GetNotificationById(id);

            if (response?.CustomCodeStatus == CustomCodeStatus.Success)
                NotificationItem = response.Data as NotificationAppUserProfileDto ?? new();

            _loading = false;
            StateHasChanged();
        }

        private void Close() => MudDialog.Close();
    }
}