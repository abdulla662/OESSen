using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Helper.Dtos.Disabilities;
using System.Net;

namespace OES.Blazor.Pages.Disabilities
{
    public partial class AddDisability
    {
        [Inject] IBlazDisabilityService DisabilityService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private AddOrUpdateDisabilityDto Model { get; set; } = new();

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrEmpty(Model.Name) ||
            string.IsNullOrEmpty(Model.Description);


        private async Task OnValidSubmitAsync()
        {
            var response = await DisabilityService.AddDisability(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/Disabilities");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/Disabilities");
    }
}
