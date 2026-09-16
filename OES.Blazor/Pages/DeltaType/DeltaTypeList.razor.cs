using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.DeltaType;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.DeltaType
{
    public partial class DeltaTypeList : ComponentBase
    {
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazAuthService AuthService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        private int Key;

        private async Task ViewDeltaTypeAsync(long deltaTypeId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DeltaTypeViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var deltaTypeModel = await BlazDeltaTypeService.GetDeltaTypeById(deltaTypeId);

            if (deltaTypeModel == null)
            {
                Snackbar.Add(Resource.AnUnExpectedErrorOccurred, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<DeltaTypeDetails>
    {
        { x => x.Model, deltaTypeModel }
    };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            await DialogService.ShowAsync<DeltaTypeDetails>(string.Empty, parameters, options);
        }

        private async Task DeleteDeltaTypeAsync(long deltaTypeId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.DeltaTypeDeleter);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisDeltaType },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                Resource.DeleteDeltaType,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazDeltaTypeService.SoftDeleteDeltaType(deltaTypeId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Key--;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }
    }
}
