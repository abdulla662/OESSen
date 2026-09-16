using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.OrganizationStructure
{
    public partial class OrganizationStructureList : ComponentBase
    {
        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private CRUD_Dto CrudDto { get; set; }

        private int listItemReloadingKey;

        private async Task DeleteOrganizationStructureRootAsync()
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.OrganizationStructureRootDeletionConfirmation },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteOrganizationStructureRoot, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var id = CrudDto.Id;

                var response = await BlazOrganizationStructureService.DeleteOrganizationStructureRootAsync(long.Parse(id.ToString()));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    listItemReloadingKey++;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                    StateHasChanged();
                }
            }
        }
    }
}
