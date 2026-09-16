using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudExtensions;
using MudExtensions.Utilities;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FifthStep;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FirstStep;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SecondStep;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.ThirdStep;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent
{
    public partial class PaperStepper : IAsyncDisposable
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazPaperService BlazPaperService { get; set; }
        [Inject] private CRUD_Dto CrudDto { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private PaperMetadata PaperMetadataFirstStepComponent { get; set; } = null!;
        private ItemBanksOrBlocksSelectionContainer ItemBanksOrBlocksSelectionSecondStepComponent { get; set; } = null!;
        private QuestionsSelectionContainer QuestionsSelectionContainerThirdStepComponent { get; set; } = null!;
        private QuestionsOrBlocksSectioningContainer QuestionsOrBlocksSectioningFourthStepComponent { get; set; } = null!;
        private MarkingScheme MarkingSchemeFifthStepComponent { get; set; } = null!;

        private PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private MudStepperExtended _stepper = new();
        private bool _addResultStep = true;
        private bool _loading;
        private bool _showNextButton = true;
        private bool _showPreviousButton = true;
        private bool _linear = true;
        private StepperOperationalMode _stepperCurrentOperationalMode = StepperOperationalMode.InsertionMode;
        private int _thirdStepChangingKey;
        private int _secondStepChangingKey;
        private int _fourthStepChangingKey;
        private int _fifthStepChangingKey;
        private int _sixthStepChangingKey;
        private HeaderTextView _view = HeaderTextView.All;
        private PaperStepperFormsMode _paperStepperFormsMode = PaperStepperFormsMode.Default;
        private CancellationTokenSource _lockRenewalCts;
        private Task _lockRenewalTask = Task.CompletedTask;
        private string _paperLockSessionId;
        private bool _paperLockAcquired;
        private static readonly TimeSpan PaperLockRenewalInterval = TimeSpan.FromSeconds(20);


        protected override async Task OnInitializedAsync()
        {
            CrudDto.IsCreatePage = await BlazSessionStorageService.GetValue<bool>("IsCreatePage");

            PaperMetadataResultedParamsDto.PaperId = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            if (!CrudDto.IsCreatePage && PaperMetadataResultedParamsDto.PaperId > 0)
            {
                _stepperCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                if (!await EnsurePaperLockAsync(PaperMetadataResultedParamsDto.PaperId))
                {
                    NavigateToUserPapersAfterLockDenied();
                    return;
                }
            }

            var paperStepperFormsModeValue = await BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
            _paperStepperFormsMode = (PaperStepperFormsMode)paperStepperFormsModeValue;

            StateHasChanged();
        }

        private async Task<bool> CheckChangeAsync(StepChangeDirection direction, int targetIndex)
        {
            if (direction == StepChangeDirection.Backward)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                if (targetIndex == 0 && PaperMetadataResultedParamsDto.PaperId > 0)
                {
                    PaperMetadataFirstStepComponent.DisableCriticalPaperMetadataFields();
                }

                if (targetIndex == 1 && (_paperStepperFormsMode != PaperStepperFormsMode.Default))
                {
                    _secondStepChangingKey--;
                }

                if (targetIndex == 2 &&
                    PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard &&
                    PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual
                )
                {
                    _thirdStepChangingKey--;
                }

                if (_stepper.GetActiveIndex() == 3 && PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive)
                {
                    bool confirmGoBack = await ShowUnsavedChangesWarningAsync();

                    if (!confirmGoBack)
                    {
                        _loading = false;
                        TogglePreviousAndNextButtonVisibilityTogether();
                        StateHasChanged();

                        return true;
                    }
                }

                if (targetIndex == 3 &&
                    PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard &&
                    PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual
                )
                {
                    _fourthStepChangingKey--;
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return false;
            }

            if (_stepper?.GetActiveIndex() == 0)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = false;

                bool shouldProceedToSecondStep;

                if (PaperMetadataResultedParamsDto.PaperId == 0)
                {
                    shouldProceedToSecondStep = await ShouldProceedToSecondStepAsync(PaperMetadataFirstStepComponent.GetCurrentPaperType());

                    StateHasChanged();
                }
                else
                {
                    shouldProceedToSecondStep = true;
                }

                if (shouldProceedToSecondStep)
                {
                    canProceed = await PaperMetadataFirstStepComponent.OnFirstStepPaperMetadataSubmitAsync();

                    if (canProceed)
                    {
                        PaperMetadataResultedParamsDto = new(PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.PaperId,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.PaperName,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.FormId,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.PaperType,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.QuestionSelectionType,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.PaperCreationStatus,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.QuestionsCount,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.PaperExamDuration,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.TotalExamMark,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.DifficultyProfileId,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.LanguageId,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.LanguageName,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.OutputFormsCount,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.AllowInstantResult,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.UsesExcelQuestionsImport,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.StageCount,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.QuestionDistributionTypeInForm,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.AdaptiveSubtype,
                                                             PaperMetadataFirstStepComponent._addPaperMetadataResponseDto.IsStepPlus);

                        await EnsureContinueModeAsync();

                        if (!await EnsurePaperLockAsync(PaperMetadataResultedParamsDto.PaperId))
                        {
                            NavigateToUserPapersAfterLockDenied();
                            return true;
                        }

                        _secondStepChangingKey--;
                    }
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !(shouldProceedToSecondStep && canProceed);
            }
            else if (_stepper?.GetActiveIndex() == 1)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = false;

                if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard)
                {
                    canProceed = await ItemBanksOrBlocksSelectionSecondStepComponent.ItemBanksSelectionComponentRef.OnSecondStepItemBanksListSubmitAsync();
                }
                else if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive)
                {
                    canProceed = await ItemBanksOrBlocksSelectionSecondStepComponent.BlocksSelectionComponentRef.OnSecondStepBlocksSelectionSubmitAsync();
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                if (canProceed)
                {
                    if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive)
                    {
                        // You might need to fill BlocksSelectionResultedParamsDto here, for example!
                    }

                    _thirdStepChangingKey--;
                }

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 2)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = false;

                if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard && PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual)
                {
                    canProceed = await QuestionsSelectionContainerThirdStepComponent.ManualQuestionsSelectionComponentRef.OnThirdStepQuestionsManualSelectionSubmitAsync();
                }
                else
                {
                    canProceed = true;
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                if (canProceed)
                {
                    _fourthStepChangingKey--;
                }

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 3)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                bool canProceed = false;

                if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard)
                {
                    if (PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual)
                    {
                        canProceed = await QuestionsOrBlocksSectioningFourthStepComponent.ManuallySelectedQuestionsSectioningComponentRef.OnFourthStepManualQuestionsSectioningSubmitAsync();
                    }
                    else if (PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Auto)
                    {
                        canProceed = await QuestionsOrBlocksSectioningFourthStepComponent.AutoSelectedQuestionsSectioningComponentRef.OnFourthStepAutoQuestionsSectioningSubmitAsync();
                    }
                }
                else if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive)
                {
                    if (PaperMetadataResultedParamsDto.AdaptiveSubtype == AdaptivePaperSubtype.MST)
                    {
                        canProceed = await QuestionsOrBlocksSectioningFourthStepComponent.MSTBlocksDistributionDropZoneComponentRef.OnFourthStepBlockDistributionDropZoneSubmitAsync();
                    }
                    else if (PaperMetadataResultedParamsDto.AdaptiveSubtype == AdaptivePaperSubtype.STEP)
                    {
                        canProceed = await QuestionsOrBlocksSectioningFourthStepComponent.STEPBlocksDistributionDropZoneComponentRef.OnFourthStepBlockDistributionDropZoneSubmitAsync();
                    }
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                if (canProceed)
                {
                    _fifthStepChangingKey--;
                }

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 4)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = await MarkingSchemeFifthStepComponent.OnFifthStepMarkingSchemeSubmitAsync();

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                if (canProceed)
                {
                    _sixthStepChangingKey--;
                }

                return !canProceed;
            }
            else if (_stepper?.GetActiveIndex() == 5)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var finishingStatus = await ShouldFinishStepperAsync();

                var paperCreationStatusChangedSuccessfully = false;

                if (finishingStatus)
                {
                    var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(PaperMetadataResultedParamsDto.PaperId, PaperCreationStatus.PaperCreated);

                    var response = await BlazPaperService.UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        paperCreationStatusChangedSuccessfully = true;
                    }
                }

                _loading = false;

                if (finishingStatus && paperCreationStatusChangedSuccessfully)
                {
                    _showPreviousButton = false; // To make sure that the previous button is hidden
                    await ReleaseCurrentPaperLockAsync(removeSessionId: true);
                }
                else
                {
                    TogglePreviousAndNextButtonVisibilityTogether();
                }

                StateHasChanged();

                return !(finishingStatus && paperCreationStatusChangedSuccessfully);
            }
            else
            {
                return true;
            }
        }

        private async Task<bool> ShowUnsavedChangesWarningAsync()
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.UnsavedDistributionChangesWarning},
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Check },
                { p => p.SubmitButtonColor, Color.Warning }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            return !result.Canceled;
        }

        private async Task<bool> ShouldProceedToSecondStepAsync(PaperType selectedPaperType)
        {
            var notificationMessage = selectedPaperType == PaperType.Standard
                ? Resource.ImportantNotificationMessage_Standard
                : Resource.ImportantNotificationMessage_Adaptive;

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, notificationMessage },
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Check },
                { p => p.SubmitButtonColor, Color.Warning }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                return true;
            }

            return false;
        }

        private async Task<bool> ShouldFinishStepperAsync()
        {
            var modeWord = _stepperCurrentOperationalMode == StepperOperationalMode.InsertionMode ? Resource.Create : Resource.Update;

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, $"{Resource.AreYouSureYouWantToFinishTo} {modeWord} {Resource.ThisPaper}"},
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Check },
                { p => p.SubmitButtonColor, Color.Warning }
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

        private void OnBreakpointChanged(Breakpoint breakpoint)
        {
            bool isSmall = breakpoint <= Breakpoint.Sm;

            _view = isSmall ? HeaderTextView.OnlyActiveText : HeaderTextView.All;
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

        private async Task EnsureContinueModeAsync()
        {
            // After completing step 1 for single-form flows,
            // a page refresh incorrectly starts a new form instead of continuing the current one.
            // This forces the stepper to resume the in-progress form.

            if (_paperStepperFormsMode is PaperStepperFormsMode.AddNormalSingleManualForm
                or PaperStepperFormsMode.AddExcelSheetSingleManualForm
                or PaperStepperFormsMode.AddSingleAutoForm)
            {
                _paperStepperFormsMode = PaperStepperFormsMode.ContinuePendingForm;
                await BlazSessionStorageService.SetValue(nameof(PaperStepperFormsMode), (int)_paperStepperFormsMode);
            }
        }

        private async Task<bool> EnsurePaperLockAsync(long paperId)
        {
            if (paperId <= 0)
            {
                return true;
            }

            if (_paperLockAcquired && PaperMetadataResultedParamsDto.PaperId == paperId)
            {
                return true;
            }

            var sessionId = await GetOrCreatePaperLockSessionIdAsync(paperId);
            var response = await BlazPaperService.AcquirePaperLockAsync(new PaperLockRequestDto(paperId, sessionId));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            _paperLockSessionId = sessionId;
            _paperLockAcquired = true;
            StartPaperLockRenewal(paperId);

            return true;
        }

        private async Task<string> GetOrCreatePaperLockSessionIdAsync(long paperId)
        {
            var storageKey = GetPaperLockSessionStorageKey(paperId);
            var sessionId = await BlazSessionStorageService.GetValue<string>(storageKey);

            if (!string.IsNullOrWhiteSpace(sessionId) && sessionId != "0")
            {
                return sessionId;
            }

            sessionId = $"{Guid.NewGuid():N}";
            await BlazSessionStorageService.SetValue(storageKey, sessionId);

            return sessionId;
        }

        private void StartPaperLockRenewal(long paperId)
        {
            if (_lockRenewalCts is not null)
            {
                return;
            }

            _lockRenewalCts = new CancellationTokenSource();
            _lockRenewalTask = RenewPaperLockUntilCancelledAsync(paperId, _lockRenewalCts.Token);
        }

        private async Task RenewPaperLockUntilCancelledAsync(long paperId, CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(PaperLockRenewalInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var response = await BlazPaperService.RenewPaperLockAsync(new PaperLockRequestDto(paperId, _paperLockSessionId));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        continue;
                    }

                    _paperLockAcquired = false;
                    await InvokeAsync(() =>
                    {
                        Snackbar.Add(Resource.LockedByAnotherUser, Severity.Error);
                        NavigationManager.NavigateTo("/UserPapers");
                    });

                    return;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ReleaseCurrentPaperLockAsync(bool removeSessionId)
        {
            _lockRenewalCts?.Cancel();

            try
            {
                await _lockRenewalTask;
            }
            catch (OperationCanceledException)
            {
            }

            _lockRenewalCts?.Dispose();
            _lockRenewalCts = null;

            if (_paperLockAcquired &&
                PaperMetadataResultedParamsDto.PaperId > 0 &&
                !string.IsNullOrWhiteSpace(_paperLockSessionId))
            {
                await BlazPaperService.ReleasePaperLockAsync(new PaperLockRequestDto(PaperMetadataResultedParamsDto.PaperId, _paperLockSessionId));
                _paperLockAcquired = false;
            }

            if (removeSessionId && PaperMetadataResultedParamsDto.PaperId > 0)
            {
                await BlazSessionStorageService.RemoveValue(GetPaperLockSessionStorageKey(PaperMetadataResultedParamsDto.PaperId));
            }
        }

        private void NavigateToUserPapersAfterLockDenied()
        {
            Snackbar.Add(Resource.LockedByAnotherUser, Severity.Error);
            NavigationManager.NavigateTo("/UserPapers");
        }

        private static string GetPaperLockSessionStorageKey(long paperId)
        {
            return $"PaperLockSessionId:{paperId}";
        }

        public async ValueTask DisposeAsync()
        {
            await ReleaseCurrentPaperLockAsync(removeSessionId: false);
        }
    }

    public sealed record PaperMetadataResultedParamsDto
    {
        public long PaperId { get; set; }

        public string PaperName { get; private init; }

        public long FormId { get; set; }

        public PaperType SelectedPaperType { get; private init; }

        public QuestionSelectionType SelectedQuestionSelectionType { get; private init; }

        public PaperCreationStatus PaperCreationStatus { get; private init; }

        public int QuestionsCount { get; private init; }

        public long OutputFormsCount { get; private init; }

        public float PaperExamDuration { get; private init; }

        public double TotalExamMark { get; private init; }

        public long? DifficultyProfileId { get; private init; }

        public long LanguageId { get; private init; }

        public string LanguageName { get; private init; }

        public bool AllowInstantResult { get; private init; }

        public bool UsesExcelQuestionsImport { get; private init; }

        public int StageCount { get; private init; }

        public QuestionDistributionTypeInForm QuestionDistributionTypeInForm { get; private init; }

        public AdaptivePaperSubtype AdaptiveSubtype { get; private init; }

        public bool IsStepPlus { get; private init; }

        public PaperMetadataResultedParamsDto() { }

        public PaperMetadataResultedParamsDto(long paperId,
                                              string paperName,
                                              long formId,
                                              PaperType paperType,
                                              QuestionSelectionType questionSelectionType,
                                              PaperCreationStatus paperCreationStatus,
                                              int questionsCount,
                                              float paperExamDuration,
                                              double totalExamMark,
                                              long? difficultyProfileId,
                                              long languageId,
                                              string languageName,
                                              long outputFormsCount,
                                              bool allowInstantResult,
                                              bool usesExcelQuestionsImport,
                                              int stageCount,
                                              QuestionDistributionTypeInForm questionDistributionTypeInForm,
                                              AdaptivePaperSubtype adaptiveSubtype = AdaptivePaperSubtype.MST,
                                              bool isStepPlus = false)
        {
            PaperId = paperId;
            PaperName = paperName;
            FormId = formId;
            SelectedPaperType = paperType;
            SelectedQuestionSelectionType = questionSelectionType;
            PaperCreationStatus = paperCreationStatus;
            QuestionsCount = questionsCount;
            OutputFormsCount = outputFormsCount;
            PaperExamDuration = paperExamDuration;
            TotalExamMark = totalExamMark;
            DifficultyProfileId = difficultyProfileId;
            LanguageId = languageId;
            LanguageName = languageName;
            AllowInstantResult = allowInstantResult;
            UsesExcelQuestionsImport = usesExcelQuestionsImport;
            StageCount = stageCount;
            QuestionDistributionTypeInForm = questionDistributionTypeInForm;
            AdaptiveSubtype = adaptiveSubtype;
            IsStepPlus = isStepPlus;
        }
    }
}