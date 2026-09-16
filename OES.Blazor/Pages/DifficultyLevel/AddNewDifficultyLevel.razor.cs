using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using System.Net;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;

namespace OES.Blazor.Pages.DifficultyLevel
{
    public partial class AddNewDifficultyLevel : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }

        private List<ProfileDto> FetchedProfiles { get; set; } = new();
        private ProfileDto SelectedProfile { get; set; } = new();
        private AddDifficultyLevelDto Model { get; set; } = new();
        private GetDeltaTypeDto SelectedDeltaType { get; set; }
        private List<GetDeltaTypeDto> FetchedDeltaTypes { get; set; } = [];

        private bool CanProceedWithInsertedDeltaValues => (Model.FromDelta > 0 || Model.ToDelta > 0) && Model.FromDelta < Model.ToDelta;

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            (Model.FromDelta < 0 || Model.FromDelta > 1) ||
            (Model.ToDelta < 0 || Model.ToDelta > 1) ||
            (SelectedProfile is null || SelectedProfile?.Id <= 0) ||
            (SelectedDeltaType is null || SelectedDeltaType?.Id <= 0);

        protected override async Task OnInitializedAsync()
        {
            FetchedProfiles.AddRange(await BlazProfileService.GetProfiles());

            FetchedDeltaTypes = await BlazDeltaTypeService.GetDeltaTypes();
        }

        private async Task OnSubmitAsync()
        {
            if (!CanProceedWithInsertedDeltaValues)
            {
                Snackbar.Add(Resource.ToDeltaValueShouldBeGreaterThanFromDeltaValue, Severity.Error);

                return;
            }

            if (SelectedProfile == null)
            {
                Snackbar.Add(Resource.PleaseSelectADifficultyProfile, Severity.Error);

                return;
            }

            Model.DifficultyProfileId = SelectedProfile.Id;

            Model.DeltaTypeId = SelectedDeltaType.Id;

            Model.OrganizationId = GlobalUserContext.CurrentOrganizationId;

            var response = await BlazDifficultyLevelService.AddDifficultyLevel(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/DifficultyLevel");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void GetSelectedProfile(ProfileDto profileDto)
        {
            SelectedProfile = profileDto;
        }

        private async Task<IEnumerable<ProfileDto>> SearchDifficultyLevelProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
                return FetchedProfiles;

            return await Task.Run(() => FetchedProfiles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList(), cancellationToken);
        }

        private void Cancel() => NavigationManager.NavigateTo("/DifficultyLevel");
    }
}
