using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Question.QuestionTypes.ComprehensionComponents
{
    public partial class ComprehensionSegmentWithAudioAnswerQuestions
    {
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public ComprehensionSubQuestionDto Model { get; set; }
        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public EventCallback<ComprehensionSubQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams ModelAnswerEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }

        private bool _processing = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;


        protected override void OnInitialized()
        {
            InitializeTextEditors();
        }

        private void InitializeTextEditors()
        {
            BodyEditorParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = Model.SubQuestionDetailsDto.Body,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                IsReadOnly = false
            };

            ModelAnswerEditorParams = new()
            {
                Label = Resource.QuestionGuideAnswer,
                InitialContent = Model.SubQuestionDetailsDto.ModelAnswer,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true
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

        private async Task<bool> ValidateFormAsync()
        {
            BodyEditorParams.ErrorShown = string.IsNullOrWhiteSpace(await BodyEditorParams.GetTextEditorContentAsync());

            ModelAnswerEditorParams.ErrorShown = string.IsNullOrWhiteSpace(await ModelAnswerEditorParams.GetTextEditorContentAsync());

            return !(BodyEditorParams.ErrorShown || ModelAnswerEditorParams.ErrorShown);
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

            Model.SubQuestionDetailsDto.Body = await BodyEditorParams.GetTextEditorContentAsync();
            Model.SubQuestionDetailsDto.ModelAnswer = await ModelAnswerEditorParams.GetTextEditorContentAsync();
            Model.SubQuestionDetailsDto.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }
    }
}
