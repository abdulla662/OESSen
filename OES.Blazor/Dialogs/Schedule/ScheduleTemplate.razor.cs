using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Schedule
{
    public partial class ScheduleTemplate
    {
        [Inject] IBlazScheduleService BlazScheduleService { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public EventCallback<long> OnScheduleSelected { get; set; }

        private int listChangingKey;

        private void GetScheduleById(long scheduleTemplateId)
        {
            OnScheduleSelected.InvokeAsync(scheduleTemplateId);

            MudDialog.Close(DialogResult.Ok(scheduleTemplateId));
        }

        private async Task DeleteScheduleMetadataTemplateAsync(object scheduleTemplateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _ = long.TryParse(scheduleTemplateId.ToString(), out long parsedId);

                var response = await BlazScheduleService.DeleteScheduleMetadataTemplateAsync(parsedId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    listChangingKey--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }

            StateHasChanged();
        }

        private void Close() => MudDialog.Cancel();
    }
}
