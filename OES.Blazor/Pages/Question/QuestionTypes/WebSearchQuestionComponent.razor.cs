using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Question.WebSearchQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class WebSearchQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];
        private bool CanAddResult =>
            !string.IsNullOrWhiteSpace(_newResult.Title) &&
            !string.IsNullOrWhiteSpace(_newResult.Url) &&
            Regex.IsMatch(_newResult.Url, RegularExpressions.UrlCheck);

        private WebSearchPropertiesDto _webSearchProperties = new();
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private string _newKeyword = string.Empty;
        private WebSearchResultDto _newResult = new();
        private bool _addingResult = false;
        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();

            if (!string.IsNullOrWhiteSpace(Model.ModelAnswer))
            {
                _webSearchProperties = JsonSerializer.Deserialize<WebSearchPropertiesDto>(
                    Model.ModelAnswer,
                    _jsonSerializerOptions
                );
            }
        }

        private void InitializeTextEditors()
        {
            BodyEditorParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = Model.Body,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
            };

            InstructionsEditorParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = false,
            };

            StateHasChanged();
        }

        private async Task<bool> ValidateFormAsync()
        {
            BodyEditorParams.ErrorShown = string.IsNullOrWhiteSpace(await BodyEditorParams.GetTextEditorContentAsync());

            return !(SelectedLanguage.Id == 0 ||
                   BodyEditorParams.ErrorShown ||
                   _webSearchProperties.Keywords.Count == 0 ||
                   _webSearchProperties.Results.Count == 0 ||
                   !_webSearchProperties.Results.Any(r => r.IsCorrect));
        }

        public async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;

            _processing = true;

            bool hasDuplicateUrls = _webSearchProperties
                .Results
                .GroupBy(r => r.Url.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (hasDuplicateUrls)
            {
                Snackbar.Add(Resource.DuplicateUrlInResults, Severity.Error);
                _processing = false;
                return;
            }

            var isFormValid = await ValidateFormAsync();

            if (!isFormValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            Model.LanguageId = SelectedLanguage.Id;
            Model.Body = string.Empty;

            Model.ModelAnswer = JsonSerializer.Serialize(
                _webSearchProperties,
                _jsonSerializerOptions
            );

            Model.Body = await BodyEditorParams.GetTextEditorContentAsync();
            Model.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private void ConfirmAddResult()
        {
            var trimmedUrl = _newResult.Url.Trim();

            if (_webSearchProperties.Results.Any(r => r.Url.Equals(trimmedUrl, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.DuplicateUrlInResults, Severity.Error);
                return;
            }

            _webSearchProperties.Results.Add(new WebSearchResultDto
            {
                Title = _newResult.Title.Trim(),
                Url = trimmedUrl,
                Description = _newResult.Description.Trim(),
                IsCorrect = _newResult.IsCorrect,
                SortOrder = _webSearchProperties.Results.Count
            });

            _addingResult = false;
            _newResult = new();
        }

        private void RemoveResult(WebSearchResultDto result)
        {
            _webSearchProperties.Results.Remove(result);
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private async Task SaveInstructionAsTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(
                Resource.SaveAsInstructionTemplate,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (result.Canceled) return;

            var templateName = result.Data?.ToString();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                Snackbar.Add(Resource.NameRequired, Severity.Error);
                return;
            }

            var instructionsContent = await InstructionsEditorParams.GetTextEditorContentAsync();

            if (string.IsNullOrWhiteSpace(instructionsContent))
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return;
            }

            var dto = new QuestionInstructionTemplateDto
            {
                Name = templateName,
                Content = instructionsContent
            };

            var response = await BlazInstructionTemplateService.SaveInstructionTemplateAsync(dto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task LoadInstructionTemplate()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<InstructionTemplateDialog>(
                Resource.SelectTemplate,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is QuestionInstructionTemplateDto selectedTemplate)
            {
                Model.Instructions = selectedTemplate.Content;

                if (InstructionsEditorParams.TextEditor != null)
                {
                    await InstructionsEditorParams.TextEditor.SetTextEditorContentAsync(selectedTemplate.Content);
                }

                Snackbar.Add(Resource.TemplateFetchedSuccessfully, Severity.Success);

                StateHasChanged();
            }
        }

        private void RemoveBodyMedia()
        {
            Model.AttachmentFileName = null;
            StateHasChanged();
        }


        #region Helper Methods

        private void OnKeywordKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == "," || e.Key == ";")
            {
                CommitTokens(_newKeyword, _webSearchProperties.Keywords);
                _newKeyword = string.Empty;
            }
        }

        private void OnKeywordBlur(FocusEventArgs _)
        {
            CommitTokens(_newKeyword, _webSearchProperties.Keywords);
            _newKeyword = string.Empty;
        }

        private void RemoveKeyword(string keyword)
        {
            _webSearchProperties.Keywords.Remove(keyword);
        }

        private static void CommitTokens(string value, List<string> list)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var parts = value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part) &&
                    !list.Contains(part, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(part);
                }
            }
        }

        #endregion
    }
}
