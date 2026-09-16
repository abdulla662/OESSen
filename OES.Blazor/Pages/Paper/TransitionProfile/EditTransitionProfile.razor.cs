using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.Paper.Requests;
using System.Net;

namespace OES.Blazor.Pages.Paper.TransitionProfile
{
    public partial class EditTransitionProfile
    {
        [Inject] private IBlazTransitionProfileService TransitionProfileService { get; set; }

        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }

        [Inject] IBlazDifficultyProfileService DifficultyProfileService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private TransitionProfileUpdateRequestDto model = new();


        private List<ProfileDto> DifficultyProfiles { get; set; } = [];

        private ProfileDto SelectedDifficultyProfile { get; set; } = new();

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Description) ||
            SelectedDifficultyProfile == null ||
            SelectedDifficultyProfile.Id == 0;


        protected override async Task OnInitializedAsync()
        {
            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            model = await TransitionProfileService.GetTransitionProfileById(id);

            DifficultyProfiles = await DifficultyProfileService.GetProfiles();

            SelectedDifficultyProfile = DifficultyProfiles.FirstOrDefault(p => p.Id == model.DifficultyProfileId) ?? new();
        }

        private async Task OnValidSubmitAsync()
        {
            model.DifficultyProfileId = SelectedDifficultyProfile.Id;

            var response = await TransitionProfileService.EditTransitionProfileAsync(model);

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
