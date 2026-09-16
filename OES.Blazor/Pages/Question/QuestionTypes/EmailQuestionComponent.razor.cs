using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.EmailQuestionAnswerDto;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class EmailQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private List<string> ExpectedToEmails { get; set; } = [];
        private List<string> ExpectedCcEmails { get; set; } = [];
        private string _expectedSubject = "";
        private List<string> RequiredBodyKeywords { get; set; } = [];

        private string _toEmailInput = "";
        private string _ccEmailInput = "";
        private string _keywordInput = "";

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

            if (!string.IsNullOrWhiteSpace(Model.ModelAnswer))
            {
                try
                {
                    var existingAnswer = JsonSerializer.Deserialize<EmailQuestionAnswerDto>(
                        Model.ModelAnswer,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (existingAnswer != null)
                    {
                        ExpectedToEmails = existingAnswer.ExpectedToEmails ?? [];
                        ExpectedCcEmails = existingAnswer.ExpectedCcEmails ?? [];
                        _expectedSubject = existingAnswer.ExpectedSubject ?? "";
                        RequiredBodyKeywords = existingAnswer.RequiredBodyKeywords ?? [];
                    }
                }
                catch
                {
                    // If deserialization fails, start fresh.
                }
            }

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();
            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();
        }

        private void InitializeTextEditors()
        {
            QuestionBodyParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = Model.Body,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true
            };

            InstructionsEditorParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
            };

            StateHasChanged();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var atIndex = email.IndexOf('@');

            if (atIndex <= 0) return false;

            var domainPart = email[(atIndex + 1)..];

            var dotIndex = domainPart.LastIndexOf('.');

            if (dotIndex <= 0 || dotIndex == domainPart.Length - 1) return false;

            return new EmailAddressAttribute().IsValid(email);
        }

        private async Task<bool> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            return SelectedLanguage != null &&
                   SelectedLanguage.Id != 0 &&
                   !QuestionBodyParams.ErrorShown &&
                   ExpectedToEmails.Count > 0 &&
                   !string.IsNullOrWhiteSpace(_expectedSubject);
        }

        public async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;
            _processing = true;

            var isFormValid = await ValidateFormAsync();

            if (!isFormValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            Model.LanguageId = SelectedLanguage.Id;
            Model.Body = await QuestionBodyParams.GetTextEditorContentAsync();
            Model.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            // Flush any pending typed-but-not-confirmed inputs
            FlushToEmail();
            FlushCcEmail();
            FlushKeyword();

            var answerDto = new EmailQuestionAnswerDto
            {
                ExpectedToEmails = ExpectedToEmails,
                ExpectedCcEmails = ExpectedCcEmails,
                ExpectedSubject = _expectedSubject.Trim(),
                RequiredBodyKeywords = RequiredBodyKeywords,
            };

            Model.ModelAnswer = JsonSerializer.Serialize(answerDto);

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private void RemoveBodyMedia()
        {
            Model.AttachmentFileName = null;
            StateHasChanged();
        }

        #region Chip Input Handlers

        private void OnToEmailKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == ",")
            {
                FlushToEmail();
            }
        }

        private void OnToEmailBlur(FocusEventArgs e)
        {
            FlushToEmail();
        }

        private void FlushToEmail()
        {
            var email = _toEmailInput?.Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(email)) return;

            if (IsValidEmail(email))
            {
                if (!ExpectedToEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
                {
                    ExpectedToEmails.Add(email);
                    _toEmailInput = "";
                    StateHasChanged();
                }
                else
                {
                    _toEmailInput = "";
                }
            }
            else
            {
                Snackbar.Add(Resource.EmailFormatInvalid, Severity.Warning);
            }
        }

        private void RemoveToEmail(string email)
        {
            ExpectedToEmails.Remove(email);
            StateHasChanged();
        }

        private void OnCcEmailKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == ",")
            {
                FlushCcEmail();
            }
        }

        private void OnCcEmailBlur(FocusEventArgs e)
        {
            FlushCcEmail();
        }

        private void FlushCcEmail()
        {
            var email = _ccEmailInput?.Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(email)) return;

            if (IsValidEmail(email))
            {
                if (!ExpectedCcEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
                {
                    ExpectedCcEmails.Add(email);
                    _ccEmailInput = "";
                    StateHasChanged();
                }
                else
                {
                    _ccEmailInput = "";
                }
            }
            else
            {
                Snackbar.Add(Resource.EmailFormatInvalid, Severity.Warning);
            }
        }

        private void RemoveCcEmail(string email)
        {
            ExpectedCcEmails.Remove(email);
            StateHasChanged();
        }

        private void OnKeywordKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == ",")
            {
                FlushKeyword();
            }
        }

        private void OnKeywordBlur(FocusEventArgs e)
        {
            FlushKeyword();
        }

        private void FlushKeyword()
        {
            var keyword = _keywordInput?.Trim().TrimEnd(',');
            if (!string.IsNullOrWhiteSpace(keyword) && !RequiredBodyKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            {
                RequiredBodyKeywords.Add(keyword);
                _keywordInput = "";
                StateHasChanged();
            }
        }

        private void RemoveKeyword(string keyword)
        {
            RequiredBodyKeywords.Remove(keyword);
            StateHasChanged();
        }

        #endregion

        #region Instruction Template

        private async Task SaveInstructionAsTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.SaveAsInstructionTemplate, parameters, options);
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

            var dialog = await DialogService.ShowAsync<InstructionTemplateDialog>(Resource.SelectTemplate, parameters, options);
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

        #endregion
    }
}