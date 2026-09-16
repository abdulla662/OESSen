using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Pages.Schedule.PaperSettingTemplates.Common
{
    public partial class PaperSettingComponent : ComponentBase
    {
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazPaperSettingsService _blazPaperSettingService { get; set; }
        [Inject] IBlazTemplateService _blazTemplateService { get; set; }
        [Inject] NavigationManager _navigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        [Parameter] public bool IsReadOnly { get; set; }
        [Parameter] public PaperSettingsTemplateRequestDto PaperSettingTemplate { get; set; } = new();
        [Parameter] public GetPaperSettingsResponseDto PaperSetting { get; set; } = new();
        [Parameter] public bool EditTemplate { get; set; } = false;
        [Parameter] public bool AdditionalSetting { get; set; } = false;

        public GetListedTemplateResponseDto SelectedDisclaimerTemplate { get; set; }
        public GetListedTemplateResponseDto SelectedOtherInstructionTemplate { get; set; }
        public GetListedTemplateResponseDto SelectedInstructionTemplate { get; set; }
        public GetListedTemplateResponseDto SelectedCertificateTemplate { get; set; }
        public GetListedTemplateResponseDto SelectedResultTemplate { get; set; }
        public GetListedTemplateResponseDto SelectedRuleTemplate { get; set; }
        public List<GetListedTemplateResponseDto> Templates { get; set; } = [];
        private List<GetListedTemplateResponseDto> InstructionTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> DisclaimerTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> ResultTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> CertificateTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> OtherInstructionTemplates { get; set; } = [];
        private List<GetListedTemplateResponseDto> RulesTemplates { get; set; } = [];

        public string TemplateName { get; set; }
        private WaterMarkOption _selectedWatermarkOption = WaterMarkOption.CandidateCode;
        private readonly List<WaterMarkOption> WatermarkOptions = Enum
            .GetValues(typeof(WaterMarkOption))
            .Cast<WaterMarkOption>()
            .ToList();
        private static IEnumerable<QuestionsDisplayMode> QuestionsSequences => GetAllQuestionsSequence();


        protected override void OnParametersSet()
        {
            if (PaperSetting != null)
            {
                SelectedCertificateTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.CertificateTemplateId);

                SelectedInstructionTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.InstructionTemplateId);

                SelectedDisclaimerTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.DisclaimerTemplateId);

                SelectedOtherInstructionTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.OtherInstructionTemplateId);

                SelectedResultTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.ResultTemplateId);

                SelectedRuleTemplate = Templates
                    .Find(t => t.Id == PaperSetting.AdditionalPaperSettings.RuleTemplateId);
            }

            if (PaperSetting.ShowWatermark)
            {
                if (PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateCode) ||
                    PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateId) ||
                    PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateName))
                {
                    _selectedWatermarkOption = Enum.Parse<WaterMarkOption>(PaperSetting.WaterMark);
                }
                else
                {
                    _selectedWatermarkOption = WaterMarkOption.Other;
                }
            }
            else
            {
                PaperSetting.WaterMark = _selectedWatermarkOption.ToString();
            }

            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            Templates = await _blazTemplateService.GetAllListedTemplatesAsync();

            if (!IsReadOnly)
            {
                InstructionTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Instruction).ToList();
                DisclaimerTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Disclaimer).ToList();
                ResultTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Result).ToList();
                CertificateTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Certificate).ToList();
                OtherInstructionTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.OtherInstructions).ToList();
                RulesTemplates = Templates?.Where(t => t.TemplateTypeId == (int)TemplateTypeEnum.Rules).ToList();
            }

            if (PaperSetting.ShowWatermark)
            {
                if (PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateCode) ||
                    PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateId) ||
                    PaperSetting.WaterMark == nameof(WaterMarkOption.CandidateName))
                {
                    _selectedWatermarkOption = Enum.Parse<WaterMarkOption>(PaperSetting.WaterMark);
                }
                else
                {
                    _selectedWatermarkOption = WaterMarkOption.Other;
                }
            }
            else
            {
                PaperSetting.WaterMark = _selectedWatermarkOption.ToString();
            }

            StateHasChanged();
        }

        private static List<QuestionsDisplayMode> GetAllQuestionsSequence()
        {
            var result = Enum
                .GetValues(typeof(QuestionsDisplayMode))
                .Cast<QuestionsDisplayMode>()
                .ToList();

            return result;
        }

        public void UpdatePaperSetting()
        {
            PaperSetting.AdditionalPaperSettings.CertificateTemplateId = SelectedCertificateTemplate?.Id ?? 0;
            PaperSetting.AdditionalPaperSettings.DisclaimerTemplateId = SelectedDisclaimerTemplate?.Id ?? 0;
            PaperSetting.AdditionalPaperSettings.InstructionTemplateId = SelectedInstructionTemplate?.Id ?? 0;
            PaperSetting.AdditionalPaperSettings.OtherInstructionTemplateId = SelectedOtherInstructionTemplate?.Id ?? 0;
            PaperSetting.AdditionalPaperSettings.ResultTemplateId = SelectedResultTemplate?.Id ?? 0;
            PaperSetting.AdditionalPaperSettings.RuleTemplateId = SelectedRuleTemplate?.Id ?? 0;
        }

        private void ChangeSelectedWaterMarkOption(WaterMarkOption option)
        {
            _selectedWatermarkOption = option;
            PaperSetting.WaterMark = option.ToString();
        }

        private async Task SaveTemplateAsync()
        {
            UpdatePaperSetting();

            PaperSettingTemplate.PaperSettingsResponseDto = PaperSetting;

            if (!string.IsNullOrWhiteSpace(TemplateName))
            {
                PaperSettingTemplate.Name = TemplateName;
            }

            var response = await _blazPaperSettingService.AddPaperSettingsTemplateAsync(PaperSettingTemplate);

            HandleResponse(response);
        }

        private async Task EditTemplateAsync()
        {
            UpdatePaperSetting();

            PaperSettingTemplate.PaperSettingsResponseDto = PaperSetting;

            var response = await _blazPaperSettingService.UpdatePaperSettingsTemplateAsync(PaperSettingTemplate);

            HandleResponse(response);
        }

        public async Task SaveOrEditTemplateAsync()
        {
            if (PaperSettingTemplate.Id == 0)
            {
                await SaveTemplateAsync();
            }
            else
            {
                await EditTemplateAsync();
            }

            _navigationManager.NavigateTo("/PaperSettingTemplates");
        }

        private void HandleResponse(ApiResponse response)
        {
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

        public void Cancel() => _navigationManager.NavigateTo("/PaperSettingTemplates");


        // Filtration Methods

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value))
                return list;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<IEnumerable<WaterMarkOption>> SearchWatermarkOptionsAsync(string value, CancellationToken token)
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

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchRuleTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RulesTemplates;

            return await FilterListAsync(RulesTemplates, t => t.Name, value);
        }

        private async Task<IEnumerable<GetListedTemplateResponseDto>> SearchCertificateTemplatesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CertificateTemplates;

            return await FilterListAsync(CertificateTemplates, t => t.Name, value);
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

        private async Task<IEnumerable<QuestionsDisplayMode>> SearchQuestionsSequencesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return QuestionsSequences;

            return await FilterListAsync(QuestionsSequences, s => s.ToString(), value);
        }
    }
}
