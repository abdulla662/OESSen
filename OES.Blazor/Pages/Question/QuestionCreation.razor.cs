using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudExtensions;
using MudExtensions.Utilities;
using OES.Blazor.Pages.Question.QuestionLanguage;
using OES.Blazor.Pages.QuestionLayout;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionCreation : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }

        private QuestionMetaData QuestionMetadataStepOne { get; set; } = new();
        private QuestionLanguageList QuestionLanguagesListStepTwo { get; set; } = new();
        private QuestionLayoutComponent QuestionLayoutComponentStepThree { get; set; } = new();
        private string QuestionType { get; set; } = string.Empty;
        private long QuestionTypeId { get; set; }
        private long QuestionMetadataId { get; set; }
        private List<SegmentQuestionDto> SegmentQuestions { get; set; } = [];

        MudStepperExtended _stepper = new();
        private bool _linear = true;
        private bool _mobileView = false;
        private bool _addResultStep = true;
        private bool _loading;
        private bool _vertical = false;
        private bool _showPreviousButton = true;
        private bool _showNextButton = true;
        const string INSERTION_MODE = nameof(INSERTION_MODE);
        const string UPDATE_MODE = nameof(UPDATE_MODE);
        string _processMode = INSERTION_MODE;
        private int _targetStep = 0;
        private bool _finishedStepper = false;
        private bool _shouldNavigateToStepTwo = false;
        private bool _isQuestionApproved = false;


        protected override async Task OnInitializedAsync()
        {
            bool isCreatePage = await BlazSessionStorageService.GetValue<bool>(MiscConstants.IsCreatePage);

            QuestionMetadataId = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformEditBtnClick);

            if (!isCreatePage && QuestionMetadataId > 0)
            {
                _processMode = UPDATE_MODE;

                var questionStatus = await BlazSessionStorageService.GetValue<int>(nameof(QuestionStatus));
                await BlazSessionStorageService.RemoveValue(nameof(QuestionStatus));

                if (questionStatus > 0 && (QuestionStatus)questionStatus == QuestionStatus.Approved)
                {
                    _isQuestionApproved = true;
                    await BlazSessionStorageService.SetValue($"{MiscConstants.IsApprovedQuestion}_{QuestionMetadataId}", "true");
                }
                else
                {
                    var approvedFlag = await BlazSessionStorageService.GetValue<string>($"{MiscConstants.IsApprovedQuestion}_{QuestionMetadataId}");
                    _isQuestionApproved = approvedFlag == "true";
                }
            }

            var targetStep = await BlazSessionStorageService.GetValue<int>(nameof(QuestionCreationStatus));

            if (targetStep == (int)QuestionCreationStatus.QuestionDetails)
            {
                await BlazSessionStorageService.RemoveValue(nameof(QuestionCreationStatus));
                _shouldNavigateToStepTwo = true;
            }

            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && _isQuestionApproved)
            {
                _stepper.Steps[0].Status = StepStatus.Completed;
                _showPreviousButton = false;
                StateHasChanged();
            }
        }

        private async Task OnStepOneDataLoaded()
        {
            if (_shouldNavigateToStepTwo)
            {
                _shouldNavigateToStepTwo = false;
                await _stepper.SetActiveIndex((int)QuestionCreationStatus.QuestionDetails);
                _stepper.Steps[0].Status = StepStatus.Completed;
                _targetStep = (int)QuestionCreationStatus.QuestionDetails;
                _showPreviousButton = false;
                StateHasChanged();
            }
        }

        public void OnQuestionCreation(long questionMetadataId)
        {
            QuestionMetadataId = questionMetadataId;
        }

        public void OnQuestionTypeChange(string questionType)
        {
            QuestionType = questionType;
        }

        public void OnQuestionTypeIdChange(long questionTypeId)
        {
            QuestionTypeId = questionTypeId;
        }

        private void OnActiveStepChanged(int newIndex)
        {
            if (_finishedStepper)
            {
                _showPreviousButton = false;
                StateHasChanged();
                return;
            }

            if (_isQuestionApproved)
            {
                _showPreviousButton = false;
                StateHasChanged();
                return;
            }

            if (_targetStep == (int)QuestionCreationStatus.QuestionDetails)
            {
                _showPreviousButton = newIndex != 1;
                StateHasChanged();
            }
        }

        private async Task<bool> CheckChangeAsync(StepChangeDirection direction, int targetIndex)
        {
            if (direction == StepChangeDirection.Backward)
            {
                if (_isQuestionApproved && targetIndex == 0)
                {
                    return true;
                }

                if (targetIndex == 0)
                {
                    await QuestionMetadataStepOne.ShouldQuestionTypeBeUpdatedAsync();
                }

                return false;
            }

            if (_stepper?.GetActiveIndex() == 0)
            {
                if (_isQuestionApproved)
                {
                    return false;
                }

                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = await QuestionMetadataStepOne.OnSubmitAsync();

                var questionStatusChangedSuccessfully = false;

                if (canProceed)
                {
                    var updateQuestionCreationStatusRequestDto = new UpdateQuestionCreationStatusRequestDto(QuestionMetadataId, ResolveNewStatus(QuestionStatus.MetadataAdded));

                    var response = await BlazQuestionService.ChangeQuestionCreationStatusAsync(updateQuestionCreationStatusRequestDto);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        questionStatusChangedSuccessfully = true;
                    }
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !(canProceed && questionStatusChangedSuccessfully);
            }
            else if (_stepper?.GetActiveIndex() == 1)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = QuestionLanguagesListStepTwo.ValidateStepTwo();

                var questionStatusChangedSuccessfully = false;

                if (canProceed)
                {
                    var updateQuestionCreationStatusRequestDto = new UpdateQuestionCreationStatusRequestDto(QuestionMetadataId, ResolveNewStatus(QuestionStatus.QuestionDetailsAdded));

                    var response = await BlazQuestionService.ChangeQuestionCreationStatusAsync(updateQuestionCreationStatusRequestDto);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        questionStatusChangedSuccessfully = true;
                    }

                    if (SegmentQuestions.Count > 0)
                    {
                        QuestionLanguagesListStepTwo.ReassignAllSegmentsMetadata();
                        await BlazQuestionService.AddOrUpdateSegmentQuestionAsync(SegmentQuestions);
                    }

                    await QuestionLayoutComponentStepThree.LoadQuestionOptions();
                }

                _loading = false;
                TogglePreviousAndNextButtonVisibilityTogether();
                StateHasChanged();

                return !(canProceed && questionStatusChangedSuccessfully);
            }
            else if (_stepper?.GetActiveIndex() == 2)
            {
                _loading = true;
                TogglePreviousAndNextButtonVisibilityTogether();
                await ScrollToTopAsync();
                StateHasChanged();

                var canProceed = QuestionLayoutComponentStepThree.ValidateStepThree();

                var questionStatusChangedSuccessfully = false;

                if (canProceed && await ShouldFinishStepperAsync())
                {
                    var updateQuestionCreationStatusRequestDto = new UpdateQuestionCreationStatusRequestDto(QuestionMetadataId, ResolveNewStatus(QuestionStatus.LayoutSelectedAndPending));

                    var response = await BlazQuestionService.ChangeQuestionCreationStatusAsync(updateQuestionCreationStatusRequestDto);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        questionStatusChangedSuccessfully = true;
                    }

                    await QuestionLayoutComponentStepThree.LoadQuestionOptions();
                }

                _loading = false;

                if (canProceed && questionStatusChangedSuccessfully)
                {
                    _finishedStepper = true;
                    _showPreviousButton = false;
                }
                else
                {
                    TogglePreviousAndNextButtonVisibilityTogether();
                }

                StateHasChanged();

                return !(canProceed && questionStatusChangedSuccessfully);
            }
            else
            {
                return true;
            }
        }

        private async Task<bool> ShouldFinishStepperAsync()
        {
            await BlazSessionStorageService.RemoveValue("PerformEditBtnClick");

            return true; // Always allow finishing without confirmation
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

        private void BackToStepOneFilledToAddAnotherQuestion()
        {
            QuestionMetadataStepOne.ResetForAnotherNewQuestion();

            QuestionLanguagesListStepTwo.ResetForAnotherNewQuestion();

            QuestionLayoutComponentStepThree.ResetForAnotherNewQuestion();

            // Stepper component reset statements:

            QuestionMetadataId = 0;

            _stepper.Reset();

            _showNextButton = true;

            _showPreviousButton = true;
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

        private void OnSegmentQuestionChanged(List<SegmentQuestionDto> segmentQuestionsList)
        {
            SegmentQuestions = segmentQuestionsList;
        }

        private QuestionStatus ResolveNewStatus(QuestionStatus defaultStatus)
        {
            return _isQuestionApproved ? QuestionStatus.Approved : defaultStatus;
        }
    }
}
