using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Schedule;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.PaperSettingTemplates
{
    public partial class PaperSettingTemplateList : ComponentBase
    {
        [Inject] private IBlazPaperSettingsService BlazPaperSettingService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private int listItemReloadingKey;

        private async Task DeleteTemplateAsync(long templateId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisTemplate },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var opts = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteTemplate, parameters, opts);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazPaperSettingService.DeletePaperSettingsTemplateAsync(templateId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    listItemReloadingKey++;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private async Task ViewTemplateAsync(long templateId)
        {
            await BlazSessionStorageService.SetCrudSessionAsync(templateId, MiscConstants.PerformViewBtnClick);

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<SchedulePaperSettingsTemplateDialog>(Resource.SchedulePaperSettingsTemplateDetails, options);

            await dialog.Result;
        }

        private async Task NavigateToEditAsync(long templateId)
        {
            await BlazSessionStorageService.SetCrudSessionAsync(templateId, MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/EditPaperSettingsTemplate");
        }
    }
}
