using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.FillBlankAnswerDto;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class FillInTheBlankComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }
        private List<string> SelectedAnswers { get; set; } = new();
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private bool _isFirstRender = true;


        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

            if (!string.IsNullOrWhiteSpace(Model.ModelAnswer))
            {
                var answersList = JsonSerializer.Deserialize<List<FillBlankAnswerDto>>(Model.ModelAnswer);

                if (answersList != null)
                {
                    SelectedAnswers = [.. answersList
                        .OrderBy(a => a.OrderId)
                        .Select(a => a.CorrectAnswer ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                    ];
                }
                else
                {
                    SelectedAnswers = [];
                }
            }

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();
        }

        private void InitializeTextEditors()
        {
            var bodyContent = Model.Body;

            if (!string.IsNullOrEmpty(bodyContent) && bodyContent.Contains("{{") && bodyContent.Contains("_}}"))
            {
                bodyContent = "";
            }

            BodyEditorParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = bodyContent,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                IsReadOnly = false,
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && _isFirstRender)
            {
                _isFirstRender = false;

                if (!string.IsNullOrEmpty(Model.Body) && Model.Body.Contains("{{") && Model.Body.Contains("_}}"))
                {
                    await Task.Delay(200);

                    var convertedBody = await JSRuntime.InvokeAsync<string>("prepareContentForEditor", Model.Body);

                    if (BodyEditorParams?.TextEditor == null)
                    {
                        return;
                    }
                    await BodyEditorParams.TextEditor.SetTextEditorContentAsync(convertedBody);
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task<bool> ValidateFormAsync()
        {
            BodyEditorParams.ErrorShown = string.IsNullOrWhiteSpace(await BodyEditorParams.GetTextEditorContentAsync());

            return !(SelectedLanguage == null || SelectedLanguage.Id == 0 || BodyEditorParams.ErrorShown);
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

            if (SelectedAnswers?.Any(a => !string.IsNullOrWhiteSpace(a)) != true)
            {
                Snackbar.Add(Resource.CannotProceedWithNoEnteredChoices, Severity.Error);
                _processing = false;
                return;
            }

            Model.LanguageId = SelectedLanguage.Id;
            var htmlContent = await BodyEditorParams.GetTextEditorContentAsync();
            Model.Body = await JSRuntime.InvokeAsync<string>("prepareContentForBackend", htmlContent);
            Model.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            var answersList = SelectedAnswers.Select((text, index) => new FillBlankAnswerDto
            {
                OrderId = index + 1,
                CorrectAnswer = text,
            }).ToList();

            Model.ModelAnswer = JsonSerializer.Serialize(answersList);

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private void OnMarkedAnswersChanged(List<string> answers)
        {
            SelectedAnswers = answers ?? [];
            Model.ModelAnswer = string.Join(" | ", SelectedAnswers);
            StateHasChanged();
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

        private void RemoveBodyMedia()
        {
            Model.AttachmentFileName = null;
            StateHasChanged();
        }

        // This method calls a JavaScript function to wrap the currently selected text in the editor as a "marked answer",
        // then handles the result: adds the text to the list of selected answers if successful, 
        // and shows appropriate notifications based on whether the editor is ready, text was selected, or an error occurred.
        public async Task MarkSelectedTextAsAnswer()
        {
            var result = await JSRuntime.InvokeAsync<WrapResult>(
                "wrapSelectedTextInEditor",
                BodyEditorParams.ComponentGuid.ToString()
            );

            switch (result.Code)
            {
                case 0: // Success: text wrapped successfully
                    var selectedText = result.Text;
                    if (!string.IsNullOrEmpty(selectedText) && !SelectedAnswers.Contains(selectedText))
                    {
                        SelectedAnswers.Add(selectedText);
                        Model.ModelAnswer = string.Join(" | ", SelectedAnswers);
                    }
                    Snackbar.Add(Resource.TextMarkedAsAnswerSuccessfully, Severity.Success);
                    StateHasChanged();
                    break;
                case 1: // Editor instance for this componentGuid not found
                case 2: // Editor object not initialized
                case 3: // Editor HTML element not found in DOM
                case 4: // Editable area inside editor not found
                    Snackbar.Add(Resource.EditorNotReady, Severity.Error);
                    break;
                case 5: // No text selected in editor
                case 6: // Selected text is outside the editable area
                case 7: // Selected text is empty or whitespace only
                    Snackbar.Add(Resource.PleaseSelectValidTextInEditor, Severity.Warning);
                    break;
                default:
                    Snackbar.Add(Resource.UnknownWhileMarkingAnswer, Severity.Error);
                    break;
            }
        }

        private async Task ClearAnswers()
        {
            SelectedAnswers.Clear();
            Model.ModelAnswer = string.Empty;

            if (BodyEditorParams.TextEditor != null)
            {
                await JSRuntime.InvokeVoidAsync("clearAnswerStyles", BodyEditorParams.ComponentGuid.ToString());
            }

            StateHasChanged();
        }
    }

    public class WrapResult
    {
        public int Code { get; set; }

        public string Text { get; set; }
    }
}
