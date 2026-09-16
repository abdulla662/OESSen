using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.FillBlankAnswerDto;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.ResourceFiles;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes.ComprehensionComponents
{
    public partial class ComprehensionFillInTheBlankQuestions
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public ComprehensionSubQuestionDto Model { get; set; }
        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public EventCallback<ComprehensionSubQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }
        private List<string> SelectedAnswers { get; set; } = [];

        private bool _processing = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private bool _isFirstRender = true;


        protected override void OnInitialized()
        {
            InitializeTextEditors();

            if (!string.IsNullOrWhiteSpace(Model.SubQuestionDetailsDto.ModelAnswer))
            {
                var answersList = JsonSerializer.Deserialize<List<FillBlankAnswerDto>>(Model.SubQuestionDetailsDto.ModelAnswer);

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
        }

        private void InitializeTextEditors()
        {
            var bodyContent = Model.SubQuestionDetailsDto.Body;

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
                IsReadOnly = false
            };

            InstructionsEditorParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.SubQuestionDetailsDto.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                IsReadOnly = false
            };

            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && _isFirstRender)
            {
                _isFirstRender = false;

                if (!string.IsNullOrEmpty(Model.SubQuestionDetailsDto.Body) && Model.SubQuestionDetailsDto.Body.Contains("{{") && Model.SubQuestionDetailsDto.Body.Contains("_}}"))
                {
                    await Task.Delay(200);

                    var convertedBody = await JSRuntime.InvokeAsync<string>("prepareContentForEditor", Model.SubQuestionDetailsDto.Body);

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

            return !BodyEditorParams.ErrorShown;
        }

        public async Task OnSubmitAsync()
        {
            _processing = true;

            var isFormValid = await ValidateFormAsync();

            if (!isFormValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            var htmlContent = await BodyEditorParams.GetTextEditorContentAsync();
            Model.SubQuestionDetailsDto.Body = await JSRuntime.InvokeAsync<string>("prepareContentForBackend", htmlContent);
            Model.SubQuestionDetailsDto.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            var answersList = SelectedAnswers.Select((text, index) => new FillBlankAnswerDto
            {
                OrderId = index + 1,
                CorrectAnswer = text,
            }).ToList();

            Model.SubQuestionDetailsDto.ModelAnswer = JsonSerializer.Serialize(answersList);

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
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
                        Model.SubQuestionDetailsDto.ModelAnswer = string.Join(" | ", SelectedAnswers);
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
            Model.SubQuestionDetailsDto.ModelAnswer = string.Empty;

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
