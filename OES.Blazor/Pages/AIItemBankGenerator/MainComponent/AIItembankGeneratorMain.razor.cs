using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudExtensions;
using MudExtensions.Utilities;
using OES.Blazor.Pages.AIItemBankGenerator.FirstStep;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.AIItemBankGenerator.MainComponent
{
    public partial class AIItembankGeneratorMain : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private bool IsConfigurationStepLocked => _isConfigurationLocked || _generatedTree != null;
        private ItemBankLevelsCreation ItemBankLevelsCreation { get; set; }

        private MudStepperExtended _stepper = new();
        private AIItemBankConfigurationsStep _configurationsStepComponent = null!;
        private AIItemBankStepperTransferableDto _configurationData = new();
        private AIItemBankNodeDto? _generatedTree;
        private bool _isConfigurationLocked = false;
        private List<ItemLevelsDto> _existingLevels = [];
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
                _showNextButton = false;
                StateHasChanged();
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
                    return await HandleFirstStepAsync();

                case 1:
                    return await HandleSecondStepAsync();

                case 2:
                    return await HandleThirdStepAsync();

                default:
                    return true;
            }
        }

        private async Task<bool> HandleFirstStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            var configurationData = _configurationsStepComponent.OnFirstStepSubmit();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            if (configurationData == null)
            {
                return true;
            }

            _configurationData = configurationData;
            _existingLevels = _configurationsStepComponent.ExistingLevels;
            return false;
        }

        private async Task<bool> HandleSecondStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            var hasNoTree = _generatedTree == null;

            if (hasNoTree)
            {
                Snackbar.Add(Resource.PleaseGenerateItemBankFirst, Severity.Warning);
            }

            return hasNoTree;
        }

        private async Task<bool> HandleThirdStepAsync()
        {
            _loading = true;
            TogglePreviousAndNextButtonVisibility();
            await ScrollToTopAsync();
            StateHasChanged();

            _loading = false;
            TogglePreviousAndNextButtonVisibility();
            StateHasChanged();

            if (!_isConfigurationLocked)
            {
                Snackbar.Add(Resource.PleaseSaveItemBankFirst, Severity.Warning);
                return true;
            }

            _finishedStepper = true;
            _showPreviousButton = false;
            _showNextButton = false;

            StateHasChanged();

            return false;
        }

        private async Task OnItemBankGenerated(AIItemBankNodeDto tree)
        {
            _generatedTree = tree;
            await Task.CompletedTask;
        }

        private void OnGeneratingStateChanged(bool isGenerating)
        {
            _isGenerating = isGenerating;
            TogglePreviousAndNextButtonVisibility();
        }

        private void ResetStepper()
        {
            NavigationManager.NavigateTo("/AIItemBankGenerator", forceLoad: true);
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