using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Helper.Dtos.DifficultyProfile;
using System.Net;

namespace OES.Blazor.Pages.DifficultyProfile
{
    public partial class AddDifficultyProfile : ComponentBase
    {
        [Inject] IBlazDifficultyProfileService ProfileService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private AddDifficultyProfileDto Model { get; set; } = new();

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrEmpty(Model.Name) ||
            string.IsNullOrEmpty(Model.Description);


        private async Task OnValidSubmitAsync()
        {
            var response = await ProfileService.AddDifficultyProfile(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/DifficultyProfile");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/DifficultyProfile");
    }
}
