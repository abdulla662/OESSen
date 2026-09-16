using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.TransitionProfile;
using System.Net;

namespace OES.Blazor.Pages.Paper.TransitionProfile
{
    public partial class AddTransitionProfile : ComponentBase
    {
        [Inject] IBlazTransitionProfileService ProfileService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazDifficultyProfileService DifficultyProfileService { get; set; }

        private AddTransitionProfileDto Model { get; set; } = new();
        private List<ProfileDto> DifficultyProfiles { get; set; } = [];
        private ProfileDto SelectedDifficultyProfile { get; set; } = new ProfileDto();
        private bool IsSubmitButtonDisabled =>
               string.IsNullOrWhiteSpace(Model.Name) ||
               string.IsNullOrWhiteSpace(Model.Description) ||
               SelectedDifficultyProfile == null ||
               SelectedDifficultyProfile.Id == 0;


        protected override async Task OnInitializedAsync()
        {
            DifficultyProfiles = await DifficultyProfileService.GetProfiles();
        }

        private async Task OnValidSubmitAsync()
        {
            Model.DifficultyProfileId = SelectedDifficultyProfile.Id;

            var response = await ProfileService.AddTransitionProfile(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/TransitionProfiles");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/TransitionProfiles");
    }
}
