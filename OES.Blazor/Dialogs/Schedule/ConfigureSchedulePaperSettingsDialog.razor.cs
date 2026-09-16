using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Schedule
{
    public partial class ConfigureSchedulePaperSettingsDialog : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazPaperSettingsService BlazPaperSettingsService { get; set; }
        [Inject] IBlazPaperService BlazPaperService { get; set; }
        [Inject] IBlazTemplateService BlazTemplateService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public long SchedulePaperId { get; set; }
        [Parameter] public long PaperId { get; set; }

        public List<GetListedTemplateResponseDto> Templates { get; set; } = [];
        private List<GetListedTemplateResponseDto> InstructionTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> DisclaimerTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> ResultTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> CertificateTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> OtherInstructionTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> RuleTemplates { get; set; } = [];
        private PaperDurationResponseDto PaperDurationResponseDto { get; set; } = new();
        private AddOrUpdatePaperSettingsRequestDto Model { get; set; } = new();
        private PaperSettingsTemplateRequestDto PaperSettingsTemplateRequestDto { get; set; } = new();

        private GetListedTemplateResponseDto _selectedDisclaimerTemplate = new();
        private GetListedTemplateResponseDto? _selectedOtherInstructionTemplate;
        private GetListedTemplateResponseDto _selectedInstructionTemplate = new();
        private GetListedTemplateResponseDto? _selectedCertificateTemplate;
        private GetListedTemplateResponseDto? _selectedResultTemplate;
        private GetListedTemplateResponseDto? _selectedRuleTemplate;

        private static IEnumerable<QuestionsDisplayMode> QuestionsSequences => Enum.GetValues<QuestionsDisplayMode>();
        private static IEnumerable<WaterMarkOption> WatermarkOptions => Enum.GetValues<WaterMarkOption>();

        private bool _additionalSettingsVisible = false;
        private bool _isInsertionMode = false;
        private WaterMarkOption _selectedWatermarkOption = WaterMarkOption.CandidateCode;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            PaperDurationResponseDto = await BlazPaperService.GetPaperDurationByIdAsync(PaperId);
            Templates = await BlazTemplateService.GetAllListedTemplatesAsync();

            DisclaimerTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Disclaimer).ToList();
            InstructionTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Instruction).ToList();
            ResultTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Result).ToList();
            CertificateTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Certificate).ToList();
            OtherInstructionTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.OtherInstructions).ToList();
            RuleTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Rules).ToList();
            await PrepareModelDataForUpdateIfUpdateMode();
        }

        public async Task PrepareModelDataForUpdateIfUpdateMode()
        {
            var schedulePaperSettingsResponseDto = await BlazPaperSettingsService.GetPaperSettingsBySchedulePaperIdAsync(SchedulePaperId);

            _isInsertionMode = schedulePaperSettingsResponseDto == null;

            if (_isInsertionMode)
            {
                Model.SchedulePaperId = SchedulePaperId;
                return;
            }

            _additionalSettingsVisible = true;

            Model = new()
            {
                Id = schedulePaperSettingsResponseDto.Id,
                SchedulePaperId = SchedulePaperId,
                QuestionsSequence = schedulePaperSettingsResponseDto.QuestionsSequence,
                QuestionsOptionsSequence = schedulePaperSettingsResponseDto.QuestionsOptionsSequence,
                ShowProfile = schedulePaperSettingsResponseDto.ShowProfile,
                ShowInstructions = schedulePaperSettingsResponseDto.ShowInstructions,
                DisablePalletNavigation = schedulePaperSettingsResponseDto.DisablePalletNavigation,
                AnswerOptionCanBeChangedAfterAttempt = schedulePaperSettingsResponseDto.AnswerOptionCanBeChangedAfterAttempt,
                MarkForReviewQuestion = schedulePaperSettingsResponseDto.MarkForReviewQuestion,
                ShowResetAnswerOption = schedulePaperSettingsResponseDto.ShowResetAnswerOption,
                AutoSaveOnTimeUp = schedulePaperSettingsResponseDto.AutoSaveOnTimeUp,
                AutoSaveOnClosing = schedulePaperSettingsResponseDto.AutoSaveOnClosing,
                ShowScientificCalculator = schedulePaperSettingsResponseDto.ShowScientificCalculator,
                ShowWatermark = schedulePaperSettingsResponseDto.ShowWatermark,
                WaterMark = schedulePaperSettingsResponseDto.WaterMark,
                SupervisorPasswordToStartExam = schedulePaperSettingsResponseDto.SupervisorPasswordToStartExam,
                SupervisorPasswordToEndExam = schedulePaperSettingsResponseDto.SupervisorPasswordToEndExam,
                AskForCandidatePasswordAtTheEndOfTest = schedulePaperSettingsResponseDto.AskForCandidatePasswordAtTheEndOfTest,
                ReplaceNotAttemptedQuestion = schedulePaperSettingsResponseDto.ReplaceNotAttemptedQuestion,
                PassingConcept = schedulePaperSettingsResponseDto.PassingConcept,
                MinimumPassingMarks = schedulePaperSettingsResponseDto.MinimumPassingMarks,
                NegativeMarking = schedulePaperSettingsResponseDto.NegativeMarking,
                NegativeMarkPercent = schedulePaperSettingsResponseDto.NegativeMarkPercent,
                ShowAnalysis = schedulePaperSettingsResponseDto.ShowAnalysis,
                ShowPrevious = schedulePaperSettingsResponseDto.ShowPrevious,
                ShowNext = schedulePaperSettingsResponseDto.ShowNext,
                TrialsCount = schedulePaperSettingsResponseDto.TrialsCount,
                LoginToleranceTime = schedulePaperSettingsResponseDto.LoginToleranceTime,
                RunThisExamOnSecureBrowserOnly = schedulePaperSettingsResponseDto.RunThisExamOnSecureBrowserOnly,
                AdditionalPaperSettings = schedulePaperSettingsResponseDto.AdditionalPaperSettings
            };

            _selectedDisclaimerTemplate = DisclaimerTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.DisclaimerTemplateId);
            _selectedInstructionTemplate = InstructionTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.InstructionTemplateId);
            _selectedResultTemplate = ResultTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.ResultTemplateId);
            _selectedCertificateTemplate = CertificateTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.CertificateTemplateId);
            _selectedOtherInstructionTemplate = OtherInstructionTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.OtherInstructionTemplateId);
            _selectedRuleTemplate = RuleTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.RuleTemplateId);

            if (Model.ShowWatermark)
            {
                if (Model.WaterMark == nameof(WaterMarkOption.CandidateCode) ||
                    Model.WaterMark == nameof(WaterMarkOption.CandidateId) ||
                    Model.WaterMark == nameof(WaterMarkOption.CandidateName))
                {
                    _selectedWatermarkOption = Enum.Parse<WaterMarkOption>(Model.WaterMark);
                }
                else
                {
                    _selectedWatermarkOption = WaterMarkOption.Other;
                }
            }
            else
            {
                Model.WaterMark = string.Empty;
            }
        }

        private void WatermarkOptionChanged(WaterMarkOption waterMarkOption)
        {
            _selectedWatermarkOption = waterMarkOption;

            if (waterMarkOption == WaterMarkOption.Other)
            {
                Model.WaterMark = string.Empty;
            }
            else
            {
                Model.WaterMark = waterMarkOption.ToString();
            }
        }

        private void OnShowWatermarkChanged(bool isToggled)
        {
            Model.ShowWatermark = isToggled;

            if (isToggled)
            {
                if (_selectedWatermarkOption != WaterMarkOption.Other)
                {
                    Model.WaterMark = _selectedWatermarkOption.ToString();
                }
                else if (string.IsNullOrWhiteSpace(Model.WaterMark) || Model.WaterMark == "Watermark")
                {
                    Model.WaterMark = string.Empty;
                }
            }
            else
            {
                Model.WaterMark = string.Empty;
            }
        }

        private void OnDisablePalletNavigationChanged(bool value)
        {
            Model.DisablePalletNavigation = value;

            StateHasChanged();
        }

        private async Task OpenTemplateNameDialog()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.TemplateName, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                await SaveTemplateAsync(templateName);
            }
        }

        private void SynchronizeSelectedPaperSettingsTemplatesWithModel()
        {
            Model.AdditionalPaperSettings.CertificateTemplateId = _selectedCertificateTemplate?.Id > 0 ? _selectedCertificateTemplate.Id : null;
            Model.AdditionalPaperSettings.DisclaimerTemplateId = _selectedDisclaimerTemplate?.Id ?? 0;
            Model.AdditionalPaperSettings.InstructionTemplateId = _selectedInstructionTemplate?.Id ?? 0;
            Model.AdditionalPaperSettings.OtherInstructionTemplateId = _selectedOtherInstructionTemplate?.Id > 0 ? _selectedOtherInstructionTemplate.Id : null;
            Model.AdditionalPaperSettings.ResultTemplateId = _selectedResultTemplate?.Id > 0 ? _selectedResultTemplate.Id : null;
            Model.AdditionalPaperSettings.RuleTemplateId = _selectedRuleTemplate?.Id > 0 ? _selectedRuleTemplate?.Id : null;
        }

        private async Task SaveTemplateAsync(string templateName)
        {
            SynchronizeSelectedPaperSettingsTemplatesWithModel();

            PaperSettingsTemplateRequestDto.Name = templateName;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.QuestionsSequence = Model.QuestionsSequence;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.QuestionsOptionsSequence = Model.QuestionsOptionsSequence;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowProfile = Model.ShowProfile;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowInstructions = Model.ShowInstructions;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.DisablePalletNavigation = Model.DisablePalletNavigation;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.AnswerOptionCanBeChangedAfterAttempt = Model.AnswerOptionCanBeChangedAfterAttempt;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.MarkForReviewQuestion = Model.MarkForReviewQuestion;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowResetAnswerOption = Model.ShowResetAnswerOption;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.AutoSaveOnTimeUp = Model.AutoSaveOnTimeUp;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.AutoSaveOnClosing = Model.AutoSaveOnClosing;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowScientificCalculator = Model.ShowScientificCalculator;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowWatermark = Model.ShowWatermark;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.WaterMark = Model.WaterMark;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.SupervisorPasswordToStartExam = Model.SupervisorPasswordToStartExam;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.SupervisorPasswordToEndExam = Model.SupervisorPasswordToEndExam;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.AskForCandidatePasswordAtTheEndOfTest = Model.AskForCandidatePasswordAtTheEndOfTest;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ReplaceNotAttemptedQuestion = Model.ReplaceNotAttemptedQuestion;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.PassingConcept = Model.PassingConcept;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.MinimumPassingMarks = Model.MinimumPassingMarks;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.NegativeMarking = Model.NegativeMarking;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.NegativeMarkPercent = Model.NegativeMarkPercent;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowAnalysis = Model.ShowAnalysis;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowPrevious = Model.ShowPrevious;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.ShowNext = Model.ShowNext;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.TrialsCount = Model.TrialsCount;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.LoginToleranceTime = Model.LoginToleranceTime;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.RunThisExamOnSecureBrowserOnly = Model.RunThisExamOnSecureBrowserOnly;
            PaperSettingsTemplateRequestDto.PaperSettingsResponseDto.AdditionalPaperSettings = Model.AdditionalPaperSettings;

            var response = await BlazPaperSettingsService.AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else if (response.CustomCodeStatus == CustomCodeStatus.Failure)
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Info);
            }

            StateHasChanged();
        }

        private async Task ShowTemplatesAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
            };

            var dialogParams = new DialogParameters();

            var dialog = await DialogService.ShowAsync<SchedulePaperSettingsTemplatesDialog>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var settingsTemplateId = (long)result.Data;

                await PrepareModelDataBySelectedTemplateByIdAysnc(settingsTemplateId);
            }
        }


        // FORM SUBMIT METHODS

        private async Task OnSubmitButtonHitAsync()
        {
            if (_isInsertionMode)
            {
                await AddPaperSettingsAsync();
            }
            else
            {
                await UpdatePaperSettingsAsync();
            }
        }

        private async Task AddPaperSettingsAsync()
        {
            if (ValidatePaperSettings())
            {
                SynchronizeSelectedPaperSettingsTemplatesWithModel();

                var response = await BlazPaperSettingsService.AddPaperSettingsAsync(Model);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);

                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }

        private async Task UpdatePaperSettingsAsync()
        {
            if (ValidatePaperSettings())
            {
                SynchronizeSelectedPaperSettingsTemplatesWithModel();

                var response = await BlazPaperSettingsService.UpdatePaperSettingsAsync(Model);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);

                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }

        private void OnCloseButtonHit() => MudDialog.Cancel();


        // HELPER METHODS

        private async Task PrepareModelDataBySelectedTemplateByIdAysnc(long settingsTemplateId)
        {
            var response = await BlazPaperSettingsService.GetPaperSettingsTemplateByIdAsync(settingsTemplateId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _additionalSettingsVisible = true;

                var paperSettingsTemplateDto = (PaperSettingsTemplateRequestDto)response.Data;

                var paperSettingsResponseDto = paperSettingsTemplateDto.PaperSettingsResponseDto;

                Model.QuestionsSequence = paperSettingsResponseDto.QuestionsSequence;
                Model.QuestionsOptionsSequence = paperSettingsResponseDto.QuestionsOptionsSequence;
                Model.ShowProfile = paperSettingsResponseDto.ShowProfile;
                Model.ShowInstructions = paperSettingsResponseDto.ShowInstructions;
                Model.DisablePalletNavigation = paperSettingsResponseDto.DisablePalletNavigation;
                Model.AnswerOptionCanBeChangedAfterAttempt = paperSettingsResponseDto.AnswerOptionCanBeChangedAfterAttempt;
                Model.MarkForReviewQuestion = paperSettingsResponseDto.MarkForReviewQuestion;
                Model.ShowResetAnswerOption = paperSettingsResponseDto.ShowResetAnswerOption;
                Model.AutoSaveOnTimeUp = paperSettingsResponseDto.AutoSaveOnTimeUp;
                Model.AutoSaveOnClosing = paperSettingsResponseDto.AutoSaveOnClosing;
                Model.ShowScientificCalculator = paperSettingsResponseDto.ShowScientificCalculator;
                Model.ShowWatermark = paperSettingsResponseDto.ShowWatermark;
                Model.WaterMark = paperSettingsResponseDto.WaterMark;
                Model.SupervisorPasswordToStartExam = paperSettingsResponseDto.SupervisorPasswordToStartExam;
                Model.SupervisorPasswordToEndExam = paperSettingsResponseDto.SupervisorPasswordToEndExam;
                Model.AskForCandidatePasswordAtTheEndOfTest = paperSettingsResponseDto.AskForCandidatePasswordAtTheEndOfTest;
                Model.RunThisExamOnSecureBrowserOnly = paperSettingsResponseDto.RunThisExamOnSecureBrowserOnly;
                Model.ReplaceNotAttemptedQuestion = paperSettingsResponseDto.ReplaceNotAttemptedQuestion;
                Model.PassingConcept = paperSettingsResponseDto.PassingConcept;
                Model.MinimumPassingMarks = paperSettingsResponseDto.MinimumPassingMarks;
                Model.NegativeMarking = paperSettingsResponseDto.NegativeMarking;
                Model.NegativeMarkPercent = paperSettingsResponseDto.NegativeMarkPercent;
                Model.ShowAnalysis = paperSettingsResponseDto.ShowAnalysis;
                Model.ShowPrevious = paperSettingsResponseDto.ShowPrevious;
                Model.ShowNext = paperSettingsResponseDto.ShowNext;
                Model.TrialsCount = paperSettingsResponseDto.TrialsCount;
                Model.LoginToleranceTime = paperSettingsResponseDto.LoginToleranceTime;
                Model.RunThisExamOnSecureBrowserOnly = paperSettingsResponseDto.RunThisExamOnSecureBrowserOnly;
                Model.AdditionalPaperSettings = paperSettingsResponseDto.AdditionalPaperSettings;

                _selectedDisclaimerTemplate = DisclaimerTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.DisclaimerTemplateId);
                _selectedInstructionTemplate = InstructionTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.InstructionTemplateId);
                _selectedResultTemplate = ResultTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.ResultTemplateId);
                _selectedCertificateTemplate = CertificateTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.CertificateTemplateId);
                _selectedOtherInstructionTemplate = OtherInstructionTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.OtherInstructionTemplateId);
                _selectedRuleTemplate = RuleTemplates.Find(t => t.Id == Model.AdditionalPaperSettings.RuleTemplateId);

                if (Model.ShowWatermark)
                {
                    if (Model.WaterMark == nameof(WaterMarkOption.CandidateCode) ||
                        Model.WaterMark == nameof(WaterMarkOption.CandidateId) ||
                        Model.WaterMark == nameof(WaterMarkOption.CandidateName)
                    )
                    {
                        _selectedWatermarkOption = Enum.Parse<WaterMarkOption>(Model.WaterMark);
                    }
                    else
                    {
                        _selectedWatermarkOption = WaterMarkOption.Other;
                    }
                }
                else
                {
                    Model.WaterMark = string.Empty;
                }
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private bool ValidatePaperSettings()
        {
            if (Model.ShowWatermark && _selectedWatermarkOption == WaterMarkOption.Other && string.IsNullOrWhiteSpace(Model.WaterMark))
            {
                Snackbar.Add(Resource.PleaseEnterWatermarkText, Severity.Error);
                return false;
            }

            if (Model.PassingConcept && Model.MinimumPassingMarks <= 0)
            {
                Snackbar.Add(Resource.PleaseEnterValidMinimumPassingMarks, Severity.Error);
                return false;
            }

            if (Model.NegativeMarking && (Model.NegativeMarkPercent <= 0 || Model.NegativeMarkPercent > 100))
            {
                Snackbar.Add(Resource.PleaseEnterValidNegativeMarkPercent, Severity.Error);
                return false;
            }

            if (!(_selectedDisclaimerTemplate?.Id > 0 &&
                _selectedInstructionTemplate?.Id > 0)
            )
            {
                Snackbar.Add(Resource.PleaseFillAllPaperTemplates, Severity.Error);
                return false;
            }

            if (Model.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent < 0)
            {
                Snackbar.Add(Resource.EndTestButtonTimeCannotBeNegative, Severity.Error);
                return false;
            }

            if (Model.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent > 0 && Model.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent > PaperDurationResponseDto.Duration)
            {
                Snackbar.Add(Resource.EndTestButtonTimeMustBeLessThanOrEqualToPaperDuration, Severity.Error);
                return false;
            }

            return true;
        }

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(1);

            if (string.IsNullOrEmpty(value))
                return list;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async static Task<IEnumerable<QuestionsDisplayMode>> SearchQuestionsSequencesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return QuestionsSequences;

            return await FilterListAsync(QuestionsSequences, s => s.ToString(), value);
        }

        private static async Task<IEnumerable<WaterMarkOption>> SearchWatermarkOptionsAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return WatermarkOptions;

            return await FilterListAsync(WatermarkOptions, w => w.ToString(), value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchOtherInstructionTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return OtherInstructionTemplates;

            return await FilterListAsync(OtherInstructionTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchDisclaimerTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DisclaimerTemplates;

            return await FilterListAsync(DisclaimerTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchCertificateTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CertificateTemplates;

            return await FilterListAsync(CertificateTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchRuleTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RuleTemplates;

            return await FilterListAsync(RuleTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchInstructionTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return InstructionTemplates;

            return await FilterListAsync(InstructionTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchResultTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ResultTemplates;

            return await FilterListAsync(ResultTemplates, t => t.Name, value);
        }
    }
}
