using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.DifficultyProfile;
using System.Net;

namespace OES.Blazor.Pages.DifficultyProfile
{
    public partial class UpdateDifficultyProfile : ComponentBase
    {
        [Inject] IBlazDifficultyProfileService ProfileService { get; set; }

        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private DifficultyProfileDto model = new();

        private bool IsSubmitButtonDisabled =>
           string.IsNullOrEmpty(model.Name) ||
           string.IsNullOrEmpty(model.Description);


        protected override async Task OnInitializedAsync()
        {
            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            model = await ProfileService.GetProfileById(id);
        }

        private async Task OnValidSubmitAsync()
        {
            var response = await ProfileService.UpdateDifficultyProfile(model);

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
