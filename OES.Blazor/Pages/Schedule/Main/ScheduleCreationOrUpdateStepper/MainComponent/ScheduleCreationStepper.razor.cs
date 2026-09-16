using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudExtensions;
using MudExtensions.Utilities;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.FirstStep;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.FourthStep;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.SecondStep;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.ThirdStep;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.MainComponent
{
    public partial class ScheduleCreationStepper
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazScheduleService BlazScheduleService { get; set; }
        [Inject] private CRUD_Dto CrudDto { get; set; }

        private ScheduleMetadata ScheduleMetadataFirstStepComponent { get; set; } = new();
        private AddSecurityConfigurations AddSecurityConfigurationSecondStepComponent { get; set; } = new();
        private SchedulePapersList SchedulePaperListThirdStepComponent { get; set; } = new();
        private ScheduleSummary ScheduleSummaryFourthStepComponent { get; set; } = new();

        public ScheduleMetadataResultedParamsDto ScheduleMetadataResultedParamsDto { get; set; } = new();

        private MudStepperExtended _stepper = new();
        private bool _linear = true;
        private bool _mobileView = false;
        private bool _addResultStep = true;
        private bool _loading;
        private bool _vertical = false;
        private bool _showPreviousButton = true;
        private bool _showNextButton = true;

        private StepperOperationalMode _processMode = StepperOperationalMode.InsertionMode;
        private int _secondStepChangingKey;
        private int _thirdStepChangingKey;
        private int _fourthStepChangingKey;

        protected override async Task OnInitializedAsync()
        {
            CrudDto.IsCreatePage = await BlazSessionStorageService.GetValue<bool>("IsCreatePage");

            var scheduleId = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            if (!CrudDto.IsCreatePage && scheduleId > 0)
            {
                _processMode = StepperOperationalMode.UpdateMode;

                await DisableCriticalFieldsInFirstStep(scheduleId);
            }

            StateHasChanged();
        }

        private async Task<bool> CheckChangeAsync(StepChangeDirection direction, int targetIndex)
        {
            if (direction == StepChangeDirection.Backward)
            {
                if (targetIndex == 0)
                {
                    await DisableCriticalFieldsInFirstStep(ScheduleMetadataResultedParamsDto.ScheduleMetadataId);
                }

                return false;
            }

            if (_stepper?.GetActiveIndex() == 0)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = await ScheduleMetadataFirstStepComponent.OnFirstStepScheduleMetadataSubmitAsync();

                if (canProceed)
                {
                    ScheduleMetadataResultedParamsDto = new ScheduleMetadataResultedParamsDto(
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.ScheduleMetadataId,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.LanguagesIds,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.VenuesIds,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.StartDate,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.EndDate,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.StartTime,
                        ScheduleMetadataFirstStepComponent.GetScheduleMetadataResponseDto.EndTime
                    );

                    _secondStepChangingKey--;
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 1)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = await AddSecurityConfigurationSecondStepComponent.OnSecondStepSecurityConfigurationsSubmitAsync();

                if (canProceed)
                {
                    _thirdStepChangingKey--;
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 2)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = SchedulePaperListThirdStepComponent.ValidateSchedulePapersListStepToProceed();

                if (canProceed)
                {
                    _fourthStepChangingKey--;
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 3)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var finishingStatus = await ShouldFinishStepperAsync();

                var canProceed = true;

                _loading = false;

                if (finishingStatus && canProceed)
                {
                    _showPreviousButton = false; // To make sure that the previous button is hidden
                }
                else
                {
                    TogglePreviousAndNextButtonVisibilityTogether();
                }

                StateHasChanged();

                return !(finishingStatus && canProceed);
            }
            else
            {
                return true;
            }
        }

        private async Task<bool> ShouldFinishStepperAsync()
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.EnsuringSchedulePublishedBeforeFinishing},
                { p => p.SubmitText, Resource.Proceed },
                { p => p.CancelText, Resource.Wait },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.CheckCircle },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await BlazSessionStorageService.RemoveValue("PerformEditBtnClick");

                return true;
            }

            return false;
        }

        private async ValueTask ScrollToTopAsync()
        {
            await JSRuntime.InvokeVoidAsync("scrollToTop");
        }

        private void TogglePreviousAndNextButtonVisibilityTogether()
        {
            _showPreviousButton = !_showPreviousButton;
            _showNextButton = !_showNextButton;
        }

        private async Task DisableCriticalFieldsInFirstStep(long scheduleMetadataId)
        {
            var scheduleValidationParametersDto = await BlazScheduleService.GetScheduleValidationParametersAsync(scheduleMetadataId);

            if (scheduleValidationParametersDto.SchedulePapersCount > 0)
            {
                ScheduleMetadataFirstStepComponent.SetLanguagesSelectionDisablingState();
            }

            if (scheduleValidationParametersDto.AnyPaperOnScheduleHasCandidates)
            {
                ScheduleMetadataFirstStepComponent.SetVenuesSelectionDisablingState();
            }
        }

        private static StepperLocalizedStrings GetLocalizedStrings()
        {
            return new StepperLocalizedStrings()
            {
                Completed = Resource.Completed,
                Finish = Resource.Finish,
                Next = Resource.Next,
                Optional = Resource.Optional,
                Previous = Resource.Previous,
                Skip = Resource.Skip,
                Skipped = Resource.Skipped,
            };
        }
    }

    public sealed record ScheduleMetadataResultedParamsDto
    {
        public long ScheduleMetadataId { get; private init; }

        public List<long> LanguagesIds { get; private init; }

        public List<long> VenuesIds { get; private init; }

        public DateOnly StartDate { get; private init; }

        public DateOnly EndDate { get; private init; }

        public TimeOnly StartTime { get; private init; }

        public TimeOnly EndTime { get; private init; }


        public ScheduleMetadataResultedParamsDto() { }

        public ScheduleMetadataResultedParamsDto(long scheduleMetadataId,
                                                 List<long> languagesIds,
                                                 List<long> venuesIds,
                                                 DateOnly startDate,
                                                 DateOnly endDate,
                                                 TimeOnly startTime,
                                                 TimeOnly endTime)
        {
            ScheduleMetadataId = scheduleMetadataId;
            LanguagesIds = languagesIds;
            VenuesIds = venuesIds;
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}