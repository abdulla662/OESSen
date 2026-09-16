using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Globalization;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class TrueOrFalseComponent
    {
        [Inject] IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }
        [Parameter] public bool CameFromAIQuestionsGenerator { get; set; }

        private static CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;
        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private LanguageDto SelectedLanguage { get; set; }
        private List<LanguageDto> LanguagesList { get; set; } = [];
        private bool SelectedOption { get; set; }

        private long _trueChoiceId;
        private long _falseChoiceId;
        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        const string EnglishTrue = "True";
        const string ArabicTrue = "صح";
        const string EnglishFalse = "False";
        const string ArabicFalse = "خطأ";


        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

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
                RequiredAsteriskShown = true,
                IsReadOnly = Model.IsUsedInExam
            };

            InstructionsParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                IsReadOnly = Model.IsUsedInExam,
            };

            Model.Choices.ForEach(choice =>
            {
                if (choice.ChoiceText == EnglishTrue) _trueChoiceId = choice.Id;
                if (choice.ChoiceText == EnglishFalse) _falseChoiceId = choice.Id;

                if (choice.IsCorrectAnswer)
                    SelectedOption = choice.ChoiceText == EnglishTrue;
            });

            StateHasChanged();
        }

        private async Task<bool> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            var isLanguageInvalid = !CameFromAIQuestionsGenerator && SelectedLanguage.Id == 0;

            return !(isLanguageInvalid ||
                   QuestionBodyParams.ErrorShown);
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

            var isArabic = CurrentCulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);

            if (!CameFromAIQuestionsGenerator)
            {
                Model.LanguageId = SelectedLanguage.Id;
            }

            Model.Body = await QuestionBodyParams.GetTextEditorContentAsync();

            Model.Choices.Clear();

            Model.Choices.Add(new ChoiceDataDto
            {
                Id = _trueChoiceId,
                ChoiceText = EnglishTrue,
                IsCorrectAnswer = SelectedOption
            });

            Model.Choices.Add(new ChoiceDataDto
            {
                Id = _falseChoiceId,
                ChoiceText = EnglishFalse,
                IsCorrectAnswer = !SelectedOption
            });


            if (SelectedOption)
            {
                Model.ModelAnswer = isArabic ? ArabicTrue : EnglishTrue;
            }
            else
            {
                Model.ModelAnswer = isArabic ? ArabicFalse : EnglishFalse;
            }

            Model.Instructions = await InstructionsParams.GetTextEditorContentAsync();

            await FormHandler.InvokeAsync(Model);

            _processing = false;
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

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.SaveAsInstructionTemplate, parameters, options);

            var result = await dialog.Result;

            if (result.Canceled) return;

            var templateName = result.Data?.ToString();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                Snackbar.Add(Resource.NameRequired, Severity.Error);
                return;
            }

            var instructionsContent = await InstructionsParams.GetTextEditorContentAsync();

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

                if (InstructionsParams.TextEditor != null)
                {
                    await InstructionsParams.TextEditor.SetTextEditorContentAsync(selectedTemplate.Content);
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
    }
}
