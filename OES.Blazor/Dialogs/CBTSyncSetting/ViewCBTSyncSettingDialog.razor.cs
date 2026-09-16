using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.CBTSyncSettingService;
using OES.Helper.Dtos.CBTSyncSetting.Responses;
using System.Net;

namespace OES.Blazor.Dialogs.CBTSyncSetting
{
    public partial class ViewCBTSyncSettingDialog
    {
        [Inject] private IBlazCBTSyncSettingService BlazCBTSyncSettingService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public long CBTSyncSettingId { get; set; }


        private CBTSyncSettingResponseDto? _cbtSyncSetting;


        protected override async Task OnInitializedAsync()
        {
            var response = await BlazCBTSyncSettingService.GetByIdAsync(CBTSyncSettingId);

            if (response.StatusCode == HttpStatusCode.OK && response.Data is CBTSyncSettingResponseDto data)
            {
                _cbtSyncSetting = data;
            }
        }

        private static string FormatTimeToAmPm(string time)
        {
            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                var dateTime = DateTime.Today.Add(timeSpan);
                return dateTime.ToString("hh:mm tt");
            }

            return time;
        }

        private void Close() => MudDialog.Close();
    }
}
