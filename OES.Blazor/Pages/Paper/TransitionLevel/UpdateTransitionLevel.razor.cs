using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.DifficultyProfile;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.TransitionProfile;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.TransitionLevel
{
    public partial class UpdateTransitionLevel : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IBlazTransitionLevelService BlazTransitionLevelService { get; set; }

        [Inject] IBlazTransitionProfileService BlazTransitionProfileService { get; set; }

        [Inject] IBlazSessionStorageService SessionStorage { get; set; }

        [Inject] IBlazDifficultyLevelService DifficultyLevelService { get; set; }

        [Inject] IBlazQuestionCategoryService QuestionCategoryService { get; set; }


        private List<TransitionProfileDto> FetchedProfiles { get; set; } = [];
        private TransitionProfileDto SelectedProfile { get; set; } = new();
        private GetTransitionLevelDto Model { get; set; } = new();
        private List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
        private List<QuestionCategoryDto> Categories { get; set; } = [];
        private long SelectedDifficultyProfileId { get; set; }
        private DifficultyLevelDto SelectedDifficultyLevel { get; set; } = new();
        private QuestionCategoryDto SelectedCategory { get; set; } = new();
        private bool CanProceedWithInsertedDeltaValues => (Model.LowerDScore > 0 || Model.UpperDScore > 0) && Model.LowerDScore < Model.UpperDScore;
        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            Model.LowerDScore < 0 || Model.LowerDScore > 1 ||
            Model.UpperDScore < 0 || Model.UpperDScore > 1 ||
            Model.LowerDScore >= Model.UpperDScore ||
            SelectedProfile is null || SelectedProfile.Id <= 0 ||
            SelectedDifficultyLevel == null || SelectedDifficultyLevel.Id <= 0 ||
            SelectedCategory == null || SelectedCategory.Id <= 0;


        protected override async Task OnInitializedAsync()
        {
            var id = await SessionStorage.GetValue<long>("PerformEditBtnClick");

            Model = await BlazTransitionLevelService.GetTransitionLevelById(id);

            Categories = await QuestionCategoryService.GetCategories();

            SelectedCategory = Categories.FirstOrDefault(c => c.Id == Model.QuestionCategoryId) ?? new QuestionCategoryDto();

            FetchedProfiles = await BlazTransitionProfileService.GetProfiles();

            SelectedProfile = FetchedProfiles.FirstOrDefault(x => x.Id == Model.TransitionProfileId);

            if (SelectedProfile != null)
            {
                SelectedDifficultyProfileId = SelectedProfile.DifficultyProfileId;

                DifficultyLevels = await DifficultyLevelService.GetDifficultyLevelByProfileIdAsync(SelectedDifficultyProfileId);

                SelectedDifficultyLevel = DifficultyLevels.FirstOrDefault(d => d.Id == Model.DifficultyLevelId) ?? new();
            }

            StateHasChanged();
        }

        public async Task OnValidSubmitAsync()
        {
            if (!CanProceedWithInsertedDeltaValues)
            {
                Snackbar.Add(Resource.TransitionValueShouldBeGreaterThanFromTransitionValue, Severity.Error);
                return;
            }

            if (SelectedProfile is null || SelectedProfile.Id <= 0)
            {
                Snackbar.Add(Resource.TransitionProfileIsRequired, Severity.Error);
                return;
            }

            if (SelectedDifficultyProfileId <= 0)
            {
                Snackbar.Add(Resource.DifficultyProfileIsRequired, Severity.Error);
                return;
            }

            if (DifficultyLevels.Count == 0)
            {
                Snackbar.Add(Resource.NoDifficultyLevelsFoundForThisProfile, Severity.Error);
                return;
            }

            if (SelectedDifficultyLevel == null || SelectedDifficultyLevel.Id <= 0)
            {
                Snackbar.Add(Resource.DifficultyLevelIsRequired, Severity.Error);
                return;
            }

            if (SelectedCategory == null || SelectedCategory.Id <= 0)
            {
                Snackbar.Add(Resource.QuestionCategoryIsRequired, Severity.Error);
                return;
            }

            Model.TransitionProfileId = SelectedProfile.Id;
            Model.DifficultyLevelId = SelectedDifficultyLevel.Id;
            Model.QuestionCategoryId = SelectedCategory.Id;

            var response = await BlazTransitionLevelService.UpdateTransitionLevel(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.TransitionLevelUpdatedSuccessfully, Severity.Success);

                NavigationManager.NavigateTo("/TransitionLevels");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task GetSelectedProfile(TransitionProfileDto profile)
        {
            SelectedProfile = profile;

            if (profile == null)
            {
                DifficultyLevels = [];
                SelectedDifficultyLevel = new DifficultyLevelDto();
                return;
            }

            SelectedDifficultyProfileId = profile.DifficultyProfileId;

            DifficultyLevels = await DifficultyLevelService
                .GetDifficultyLevelByProfileIdAsync(SelectedDifficultyProfileId) ?? [];

            SelectedDifficultyLevel = new();
        }

        private async Task<IEnumerable<TransitionProfileDto>> SearchTransitionLevelProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
                return FetchedProfiles;

            return await Task.Run(() => FetchedProfiles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList(), cancellationToken);
        }

        private void Cancel() => NavigationManager.NavigateTo("/TransitionLevels");
    }
}
