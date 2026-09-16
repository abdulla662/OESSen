using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Question.WebRegistrationQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class WebRegistrationQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; } = default!;

        [Parameter] public QuestionDetailsDto Model { get; set; } = null!;
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; } = new();
        public TextEditorParams InstructionsEditorParams { get; set; } = new();
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private string _expectedFirstName = string.Empty;
        private string _expectedLastName = string.Empty;
        private string _expectedEmail = string.Empty;
        private string _expectedPassword = string.Empty;

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
                var expectedData = JsonSerializer.Deserialize<WebRegistrationExpectedDataDto>(Model.ModelAnswer);
                if (expectedData != null)
                {
                    _expectedFirstName = expectedData.ExpectedFirstName;
                    _expectedLastName = expectedData.ExpectedLastName;
                    _expectedEmail = expectedData.ExpectedEmail;
                    _expectedPassword = expectedData.ExpectedPassword;
                }
            }
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
                Label = Resource.Instructions,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = false
            };
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

        private async Task<(bool IsValid, string Message)> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            if (SelectedLanguage.Id == 0 || QuestionBodyParams.ErrorShown ||
                string.IsNullOrWhiteSpace(_expectedFirstName) ||
                string.IsNullOrWhiteSpace(_expectedLastName) ||
                string.IsNullOrWhiteSpace(_expectedEmail) ||
                string.IsNullOrWhiteSpace(_expectedPassword))
            {
                return (false, Resource.CannotProceedWithEmptyOrInvalidMandatoryFields);
            }

            if (!IsValidEmail(_expectedEmail))
            {
                return (false, Resource.EmailFormatInvalid);
            }

            return (true, string.Empty);
        }

        public async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;
            _processing = true;

            var validationResult = await ValidateFormAsync();

            if (!validationResult.IsValid)
            {
                Snackbar.Add(validationResult.Message, Severity.Error);
                _processing = false;
                return;
            }

            Model.LanguageId = SelectedLanguage.Id;
            Model.Body = await QuestionBodyParams.GetTextEditorContentAsync();
            Model.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            var expectedData = new WebRegistrationExpectedDataDto
            {
                ExpectedFirstName = _expectedFirstName.Trim().ToLower(),
                ExpectedLastName = _expectedLastName.Trim().ToLower(),
                ExpectedEmail = _expectedEmail.Trim().ToLower(),
                ExpectedPassword = _expectedPassword.Trim().ToLower()
            };

            Model.ModelAnswer = JsonSerializer.Serialize(expectedData);

            await FormHandler.InvokeAsync(Model);
            _processing = false;
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

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private void RemoveBodyMedia()
        {
            Model.AttachmentFileName = null;
            StateHasChanged();
        }
    }
}