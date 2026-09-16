using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Pages.AIItemBankGenerator.Dialogs;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.ItemBankLevels;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.AIItemBankGenerator.FirstStep
{
    public partial class AIItemBankConfigurationsStep
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazorItemBankLevelsService BlazorItemBankLevelsService { get; set; } = default!;
        [Inject] private IBlazQuestionLanguageService BlazLanguageService { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;

        [Parameter] public bool IsConfigurationLocked { get; set; }
        [Parameter] public ItemBankLevelsCreation SelectedLevelsCreation { get; set; }
        [Parameter] public EventCallback<ItemBankLevelsCreation> SelectedLevelsCreationChanged { get; set; }

        private ItemBankLevelsCreation _selectedLevelsCreation = ItemBankLevelsCreation.UseExisting;
        private int? _maxLevelsCount = 5;
        private int? _maxChildrenPerNode = 10;
        private IBrowserFile? _file;
        private bool _isSubmitButtonHit = false;
        private List<ItemLevelsDto> _existingLevels = [];
        private LanguageDto _selectedLanguage = new();
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        private static readonly string AllowedExtensionsString = string
            .Join(", ", Enum.GetValues<DocumentFileType>()
            .Where(e => e != DocumentFileType.Unknown)
            .Select(e => e.ToString().ToUpperInvariant()));
        public string TemplateName { get; set; } = string.Empty;
        public List<ItemLevelsDto> ExistingLevels => _existingLevels;
        private List<LanguageDto> Languages { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            var languagesTask = BlazLanguageService.GetAllLanguagesAsync();

            await LoadExistingLevelsAsync();

            Languages = await languagesTask;
        }

        private async Task LoadExistingLevelsAsync()
        {
            try
            {
                _existingLevels = await BlazorItemBankLevelsService.GetLevels() ?? [];
            }
            catch
            {
                _existingLevels = [];
            }
        }

        private async Task OnLevelsCreationChanged(ItemBankLevelsCreation value)
        {
            _selectedLevelsCreation = value;
            SelectedLevelsCreation = value;
            await SelectedLevelsCreationChanged.InvokeAsync(value);
        }

        private void OnUploadFiles(IBrowserFile file)
        {
            if (file == null) return;

            var extension = Path.GetExtension(file.Name)?.TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !Enum.TryParse<DocumentFileType>(extension, ignoreCase: true, out var docType) || docType == DocumentFileType.Unknown)
            {
                Snackbar.Add($"{Resource.UnsupportedFileType}. {Resource.SupportedFileExtensions}: {AllowedExtensionsString}", Severity.Error);
                _file = null;
                StateHasChanged();
                return;
            }

            if (file.Size > MiscConstants.AIQuestionsGenerationMaxFileSizeInBytes)
            {
                Snackbar.Add(Resource.FileSizeExceedsLimit, Severity.Error);
                return;
            }

            _file = file;
            StateHasChanged();
        }

        private void ClearFile()
        {
            _file = null;
            StateHasChanged();
        }

        public AIItemBankStepperTransferableDto? OnFirstStepSubmit()
        {
            _isSubmitButtonHit = true;

            StateHasChanged();

            if (!ValidateForm())
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return null;
            }

            return new AIItemBankStepperTransferableDto
            {
                File = _file,
                ItemBankLevelsCreation = _selectedLevelsCreation,
                MaxLevelsCount = _maxLevelsCount,
                MaxChildrenPerNode = _maxChildrenPerNode,
                SelectedLanguageDto = _selectedLanguage
            };
        }

        private bool ValidateForm()
        {
            return _selectedLanguage?.Id > 0 &&
                   _maxLevelsCount.HasValue &&
                   _maxLevelsCount.Value >= 1 &&
                   _maxLevelsCount.Value <= 50 &&
                   _maxChildrenPerNode.HasValue &&
                   _maxChildrenPerNode.Value >= 1 &&
                   _maxChildrenPerNode.Value <= 50 &&
                   _file != null;
        }

        private async Task<IEnumerable<LanguageDto>> SearchLanguageAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) || (_selectedLanguage?.Name == null && string.IsNullOrWhiteSpace(value)) || _selectedLanguage?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Languages;
            }

            return await FilterListAsync(Languages, l => l.Name, value);
        }

        private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value))
                return list;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private async Task ShowTemplateAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var parameters = new DialogParameters<ItemBankTemplate>
            {
                { p => p.IsFromItemBankAI, true }
            };

            var dialog = await DialogService.ShowAsync<ItemBankTemplate>(string.Empty, parameters, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data != null && long.TryParse(result.Data.ToString(), out long templateId))
            {
                await FillStepOneFromTemplateAsync(templateId);
            }
        }

        private async Task FillStepOneFromTemplateAsync(long templateId)
        {
            var response = await BlazItemBankService.GetItemBankTemplateById(templateId);

            if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
            {
                if (response.Data is GetAIItemBankTemplateResponseDto templateData)
                {
                    await FillItemBankMetaDataAsync(templateData.AIItemBankTemplate);
                }
                else
                {
                    var jsonString = response.Data.ToString();
                    var templateDataObj = JsonSerializer.Deserialize<GetAIItemBankTemplateResponseDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (templateDataObj?.AIItemBankTemplate != null)
                    {
                        await FillItemBankMetaDataAsync(templateDataObj.AIItemBankTemplate);
                    }
                }
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task FillItemBankMetaDataAsync(AIItemBankTemplateCreationDto templateDto)
        {
            if (templateDto.SelectedLanguage != null)
            {
                _selectedLanguage = Languages.Find(l => l.Id == templateDto.SelectedLanguage.Id) ?? templateDto.SelectedLanguage;
            }

            _selectedLevelsCreation = templateDto.ItemBankLevelsCreation;
            await OnLevelsCreationChanged(_selectedLevelsCreation);

            _maxLevelsCount = templateDto.MaxLevelsCount;
            _maxChildrenPerNode = templateDto.MaxChildrenPerNode;

            StateHasChanged();
        }

        private async Task SaveTemplateAsync()
        {
            var template = new AIItemBankTemplateCreationDto
            {
                Name = TemplateName,
                IsFromAI = true,
                SelectedLanguage = _selectedLanguage,
                ItemBankLevelsCreation = _selectedLevelsCreation,
                MaxLevelsCount = _maxLevelsCount,
                MaxChildrenPerNode = _maxChildrenPerNode
            };

            var response = await BlazItemBankService.AddTemplateAIItemBankAsync(template);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Snackbar.Add(Resource.TemplateDataHasBeenCreatedSuccessfully, Severity.Success);
                    break;

                case HttpStatusCode.Conflict:
                    Snackbar.Add(response.Message, Severity.Warning);
                    break;

                default:
                    Snackbar.Add(Resource.FailedToCreateATemplatePleaseTryEnterAValidData, Severity.Error);
                    break;
            }

            StateHasChanged();
        }

        private async Task OpenTemplateNameDialog()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Large };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.ItemBankTemplate, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                TemplateName = templateName;

                await SaveTemplateAsync();
            }
        }
    }
}
