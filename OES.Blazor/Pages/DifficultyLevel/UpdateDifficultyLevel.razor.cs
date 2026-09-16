using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.DifficultyLevel
{
    public partial class UpdateDifficultyLevel : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazDifficultyLevelService BlazDifficultyLevelService { get; set; }
        [Inject] IBlazDifficultyProfileService BlazProfileService { get; set; }
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }

        private List<ProfileDto> FetchedProfiles { get; set; } = [];
        private ProfileDto SelectedProfile { get; set; } = new();
        private UpdateDifficultyLevelDto Model { get; set; } = new();
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
            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");

            var difficultyLevel = await BlazDifficultyLevelService.GetDifficultyLevelById(id);

            if (difficultyLevel != null)
            {
                Model = new UpdateDifficultyLevelDto
                {
                    Id = difficultyLevel.Id,
                    Name = difficultyLevel.Name,
                    FromDelta = difficultyLevel.FromDelta,
                    ToDelta = difficultyLevel.ToDelta,
                    DeltaTypeId = difficultyLevel.DeltaTypeId,
                    DifficultyProfileId = difficultyLevel.DifficultyProfileId
                };
            }
            else
            {
                Snackbar.Add(Resource.DifficultyLevelNotFound, Severity.Error);

                NavigationManager.NavigateTo("/DifficultyLevel");
            }

            FetchedProfiles.AddRange(await BlazProfileService.GetProfiles());

            SelectedProfile = FetchedProfiles.Find(x => x.Id == Model.DifficultyProfileId);

            FetchedDeltaTypes = await BlazDeltaTypeService.GetDeltaTypes();

            SelectedDeltaType = FetchedDeltaTypes.Find(x => x.Id == Model.DeltaTypeId);
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

            var response = await BlazDifficultyLevelService.UpdateDifficultyLevel(Model);

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

        private void GetSelectedProfile(ProfileDto profileDto) => SelectedProfile = profileDto;

        private async Task<IEnumerable<ProfileDto>> SearchDifficultyLevelProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
                return FetchedProfiles;

            return await Task.Run(() => FetchedProfiles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList(), cancellationToken);
        }

        private void Cancel() => NavigationManager.NavigateTo("/DifficultyLevel");
    }
}
