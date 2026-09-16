using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.GroupRolesSelectionDialogs;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Extentions.DialogHelpers
{
    public static class DialogInteractionService
    {
        public static async Task<List<T>> OpenSelectionDialogAsync<T>(
           IDialogService dialogService,
           string title,
           IEnumerable<T> preSelectedItems,
           Func<PaginationSearchModel, Task<CustomTableData<T>>> endpointService,
           ResourceType resourceType,
           EventCallback<Guid> onDelete,
           bool isOpenedFromAI = false
        )
        {
            var parameters = new DialogParameters
            {
                { DialogParameterKeys.PreSelectedItems, preSelectedItems?.ToList() ?? new List<T>() },
                { DialogParameterKeys.EndpointService, endpointService },
                { DialogParameterKeys.ResourceType, resourceType },
                { DialogParameterKeys.OnConfirmDelete, onDelete },
                { DialogParameterKeys.IsOpenedFromAI, isOpenedFromAI }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true
            };

            var dialog = await dialogService.ShowAsync<GenericGroupSelectorDialog<T>>(title, parameters, options);

            var result = await dialog.Result;

            return !result.Canceled && result.Data is List<T> selected
                ? selected
                : [.. preSelectedItems];
        }

        public static async Task<bool> DeleteEntityAsync(
            Guid id,
            IDialogService dialogService,
            Func<Guid, Task<ApiResponse>> deleteService,
            ISnackbar snackbar
        )
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.Delete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await dialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (result.Canceled)
                return false;

            var response = await deleteService(id);

            snackbar.Add(response.Message,
                response.CustomCodeStatus == CustomCodeStatus.Success
                    ? Severity.Success
                    : Severity.Error);

            return response.CustomCodeStatus == CustomCodeStatus.Success;
        }
    }
}