using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.OrganizationStructure
{
    public partial class AddOrUpdateOrganizationStructureNodeDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public AddOrUpdateOrganizationStructureNodeRequestDto Model { get; set; } = new();

        private bool IsUpdateMode => Model.Id > 0;

        private JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private async Task SubmitAsync()
        {
            if (IsUpdateMode)
            {
                await UpdateOrganizationStructureNodeAsync();
            }
            else
            {
                await AddOrganizationStructureNodeAsync();
            }
        }

        private async Task AddOrganizationStructureNodeAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.AddOrganizationStructureNodeAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                var returnedNodeDto = JsonSerializer.Deserialize<AddOrUpdateOrganizationStructureNodeResponseDto>(apiResponse.Data.ToString(), _jsonSerializerOptions);

                MudDialog.Close(DialogResult.Ok(returnedNodeDto));
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
        }

        private async Task UpdateOrganizationStructureNodeAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.UpdateOrganizationStructureNodeAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                var returnedNodeDto = JsonSerializer.Deserialize<AddOrUpdateOrganizationStructureNodeResponseDto>(apiResponse.Data.ToString(), _jsonSerializerOptions);

                MudDialog.Close(DialogResult.Ok(returnedNodeDto));
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
