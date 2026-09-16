using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
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
    public partial class AddTransitionLevel : ComponentBase
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazTransitionLevelService BlazTransitionLevelService { get; set; }
        [Inject] IBlazTransitionProfileService BlazTransitionProfileService { get; set; }
        [Inject] IBlazDifficultyLevelService DifficultyLevelService { get; set; }
        [Inject] IBlazQuestionCategoryService QuestionCategoryService { get; set; }

        private List<TransitionProfileDto> FetchedProfiles { get; set; } = [];
        private TransitionProfileDto SelectedProfile { get; set; } = new();
        private AddTransitionLevelRequestDto Model { get; set; } = new();
        private List<DifficultyLevelDto> DifficultyLevels { get; set; } = [];
        private List<QuestionCategoryDto> Categories { get; set; } = [];
        private DifficultyLevelDto SelectedDifficultyLevel { get; set; } = new();
        private QuestionCategoryDto SelectedCategory { get; set; } = new();
        private long SelectedDifficultyProfileId { get; set; }
        private bool CanProceedWithInsertedTransitionValues =>
            (Model.UpperDScore > 0 || Model.LowerDScore > 0) && Model.UpperDScore > Model.LowerDScore;
        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model.Name) ||
            !CanProceedWithInsertedTransitionValues ||
            SelectedProfile?.Id <= 0 ||
            SelectedDifficultyLevel?.Id <= 0 ||
            SelectedCategory?.Id <= 0;


        protected override async Task OnInitializedAsync()
        {
            FetchedProfiles.AddRange(await BlazTransitionProfileService.GetProfiles());

            Categories = await QuestionCategoryService.GetCategories();
        }

        private async Task GetSelectedProfile(TransitionProfileDto profile)
        {
            SelectedProfile = profile;

            if (profile == null)
            {
                DifficultyLevels = [];
                SelectedDifficultyLevel = new();
                return;
            }

            SelectedDifficultyProfileId = profile.DifficultyProfileId;

            DifficultyLevels = await DifficultyLevelService.GetDifficultyLevelByProfileIdAsync(SelectedDifficultyProfileId);

            SelectedDifficultyLevel = new();

            StateHasChanged();
        }

        public async Task OnValidSubmitAsync()
        {
            if (!CanProceedWithInsertedTransitionValues)
            {
                Snackbar.Add(Resource.ToTransitionValueShouldBeGreaterThanFromTransitionValue, Severity.Error);
                return;
            }

            if (SelectedProfile is null || SelectedProfile.Id <= 0)
            {
                Snackbar.Add(Resource.TransitionProfileIsRequired, Severity.Error);
                return;
            }

            if (DifficultyLevels.Count == 0)
            {
                Snackbar.Add(Resource.NoDifficultyLevelsFoundForThisProfile, Severity.Error);
                return;
            }

            if (SelectedDifficultyLevel is null || SelectedDifficultyLevel.Id <= 0)
            {
                Snackbar.Add(Resource.DifficultyLevelIsRequired, Severity.Error);
                return;
            }

            if (SelectedCategory is null || SelectedCategory.Id <= 0)
            {
                Snackbar.Add(Resource.QuestionCategoryIsRequired, Severity.Error);
                return;
            }

            Model.TransitionProfileId = SelectedProfile.Id;
            Model.DifficultyLevelId = SelectedDifficultyLevel.Id;
            Model.QuestionCategoryId = SelectedCategory.Id;

            var response = await BlazTransitionLevelService.AddTransitionLevel(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/TransitionLevels");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task<IEnumerable<TransitionProfileDto>> SearchDifficultyLevelProfileAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
                return FetchedProfiles;

            return await Task.Run(() => FetchedProfiles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList(), cancellationToken);
        }

        private void Cancel() => NavigationManager.NavigateTo("/TransitionLevels");
    }
}
