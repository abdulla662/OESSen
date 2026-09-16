using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Paper.TransitionProfiles
{
    public partial class TransitionProfileViewDialog
    {
        [Inject] public IBlazTransitionProfileService BlazTransitionProfile { get; set; } = default!;

        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public long ProfileId { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        private TransitionProfileDto Model { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            if (ProfileId == 0)
            {
                Snackbar.Add(Resource.FailedLoadProfileDetails, Severity.Error);
                return;
            }

            await LoadProfile(ProfileId);
        }

        private async Task LoadProfile(long profileId)
        {
            var response = await BlazTransitionProfile.GetTransitionProfileById(profileId);

            if (response != null)
            {
                Model = new TransitionProfileDto
                {
                    Id = response.Id,
                    Name = response.Name,
                    Description = response.Description
                };
            }

            StateHasChanged();
        }

        private void CloseDialog()
        {
            MudDialog.Close();
        }
    }
}
