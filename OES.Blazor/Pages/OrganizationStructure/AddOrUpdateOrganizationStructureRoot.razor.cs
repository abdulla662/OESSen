using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using System.Net;

namespace OES.Blazor.Pages.OrganizationStructure
{
    public partial class AddOrUpdateOrganizationStructureRoot
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; } = default!;

        [Inject] private NavigationManager Navigation { get; set; }

        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }

        private AddOrUpdateOrganizationStructureRootRequestDto Model { get; set; } = new();

        private bool IsUpdateMode { get; set; } = false;

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            string.IsNullOrWhiteSpace(Model.Description);

        protected override async Task OnInitializedAsync()
        {
            var currentId = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            if (currentId > 0)
            {
                Model = await BlazOrganizationStructureService.GetOrganizationStructureByIdAsync(currentId);
                IsUpdateMode = true;
            }
            else
            {
                IsUpdateMode = false;
            }

            StateHasChanged();
        }

        private async Task SubmitAsync()
        {
            if (IsUpdateMode)
            {
                await UpdateOrganizationStructureRootAsync();
            }
            else
            {
                await AddOrganizationStructureRootAsync();
            }
        }

        private async Task AddOrganizationStructureRootAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.AddOrganizationStructureRootAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                Navigation.NavigateTo("/OrganizationStructureList");
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
        }

        private async Task UpdateOrganizationStructureRootAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.UpdateOrganizationStructureRootAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                Navigation.NavigateTo("/OrganizationStructureList");
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }

            await SessoinStorage.RemoveValue("PerformEditBtnClick");
        }

        private void NavigateBack()
        {
            SessoinStorage.RemoveValue("PerformEditBtnClick");
            Navigation.NavigateTo("/OrganizationStructureList");
        }
    }
}