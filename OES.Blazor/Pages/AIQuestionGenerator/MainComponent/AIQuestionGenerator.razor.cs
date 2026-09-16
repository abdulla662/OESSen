using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudExtensions;
using MudExtensions.Utilities;
using OES.Blazor.Pages.AIQuestionGenerator.FirstStep;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.AIQuestionGenerator.MainComponent
{
    public partial class AIQuestionGenerator : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private bool IsConfigurationStepLocked => _isConfigurationLocked || _generatedQuestions?.Questions?.Count > 0;

        private MudStepperExtended _stepper = new();
        private ConfigurationStep _configurationStepComponent = null!;
        private AIQuestionGenerationStepperTransferableDto _configurationData = new();
        private AIGeneratedQuestionsResultDto? _generatedQuestions;
        private bool _isConfigurationLocked = false;
        private bool _isSubmitButtonHit = false;

        private bool _linear = true;
        private bool _vertical = false;
        private bool _mobileView = false;
        private bool _loading = false;
        private bool _isGenerating = false;
        private bool _showPreviousButton = true;
        private bool _showNextButton = true;
        private bool _finishedStepper = false;

        private void OnActiveStepChanged(int newIndex)
        {
            if (_finishedStepper)
            {
                _showPreviousButton = false;
                StateHasChanged();
                return;
            }
        }

        private async Task<bool> CheckChangeAsync(StepChangeDirection direction, int targetIndex)
        {
            if (_isGenerating)
            {
                return true;
            }

            if (direction == StepChangeDirection.Backward)
            {
                return false;
            }

            var activeIndex = _stepper?.GetActiveIndex() ?? 0;

            switch (activeIndex)
            {
                case 0:
                    return await HandleConfigurationStepAsync();

                case 1:
                    return await HandleGenerateStepAsync();

                case 2:
                    return await HandleReviewStepAsync();

                default:
                    return true;
            }
        }

        private async Task<bool> HandleConfigurationStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            _isSubmitButtonHit = true;
            StateHasChanged();

            var configurationData = _configurationStepComponent.OnFirstStepSubmit();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            if (configurationData == null)
            {
                return true;
            }

            _configurationData = configurationData;
            return false;
        }

        private async Task<bool> HandleGenerateStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            var hasNoQuestions = _generatedQuestions?.Questions == null || _generatedQuestions.Questions.Count == 0;

            if (hasNoQuestions)
            {
                Snackbar.Add(Resource.PleaseGenerateQuestionsFirst, Severity.Warning);
            }

            return hasNoQuestions;
        }

        private async Task<bool> HandleReviewStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            var hasQuestions = _generatedQuestions?.Questions != null && _generatedQuestions.Questions.Count > 0;
            if (hasQuestions && !_isConfigurationLocked)
            {
                Snackbar.Add(Resource.PleaseSaveQuestionsFirst, Severity.Warning);
                return true;
            }

            _finishedStepper = true;
            _showPreviousButton = false;
            _showNextButton = false;

            StateHasChanged();

            return false;
        }

        private async Task OnQuestionsGenerated(AIGeneratedQuestionsResultDto result)
        {
            _generatedQuestions = result;
            await Task.CompletedTask;
        }

        private void OnGeneratingStateChanged(bool isGenerating)
        {
            _isGenerating = isGenerating;
            TogglePreviousAndNextButtonVisibility();
        }

        private async ValueTask ScrollToTopAsync()
        {
            await JSRuntime.InvokeVoidAsync("scrollToTop");
        }

        private void TogglePreviousAndNextButtonVisibility()
        {
            _showPreviousButton = !_showPreviousButton;
            _showNextButton = !_showNextButton;

            StateHasChanged();
        }

        private static StepperLocalizedStrings GetLocalizedStrings()
        {
            return new StepperLocalizedStrings
            {
                Completed = Resource.Completed,
                Finish = Resource.Finish,
                Next = Resource.Next,
                Optional = Resource.Optional,
                Previous = Resource.Previous,
                Skip = Resource.Skip,
                Skipped = Resource.Skipped
            };
        }
    }
}
