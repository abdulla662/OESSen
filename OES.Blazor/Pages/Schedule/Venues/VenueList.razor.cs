using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Venue;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;


namespace OES.Blazor.Pages.Schedule.Venues
{
    public partial class VenueList
    {
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazVenueService BlazVenueService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        public VenueViewDto Model { get; set; } = new VenueViewDto();
        private int VenueKey;
        private IList<IBrowserFile> files = [];
        private List<string> ErrorListDto = [];

        private async Task DeleteVenueAsync(long venueId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.VenueDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisVenue },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<GenericDialog>(
                Resource.DeleteVenue,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazVenueService.DeleteVenue(venueId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    VenueKey++;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);

                    StateHasChanged();
                }
            }
        }

        private async Task OpenViewDialog(long venueId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.VenueViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                venueId,
                MiscConstants.PerformViewBtnClick);

            var parameters = new DialogParameters<VenueViewDialog>
            {
                { x => x.Model, Model }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<VenueViewDialog>(
                Resource.VenueDetails,
                parameters,
                options);
        }

        private async Task NavigateToEditAsync(long venueId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.VenueEditor);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                venueId,
                MiscConstants.PerformEditBtnClick);

            NavigationManager.NavigateTo("/EditVenue");
        }

        private async Task OpenImportVenuesDialog()
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.VenueImporter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var dialogParams = new DialogParameters<ImportVenuesDialog>();

            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                CloseButton = true
            };

            files.Clear();

            ErrorListDto.Clear();

            var dialog = await DialogService.ShowAsync<ImportVenuesDialog>(
                string.Empty,
                dialogParams,
                dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                VenueKey--;
            }
        }
    }
}