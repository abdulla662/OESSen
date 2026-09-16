using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Question.QuestionTypes.ComprehensionComponents
{
    public partial class ComprehensionTrueOrFalseQuestion
    {
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public ComprehensionSubQuestionDto Model { get; set; }
        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public EventCallback<ComprehensionSubQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private bool SelectedOption { get; set; }
        private long TrueChoiceId { get; set; }
        private long FalseChoiceId { get; set; }

        private bool _processing = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;


        protected override void OnInitialized()
        {
            InitializeTextEditors();
        }

        private void InitializeTextEditors()
        {
            QuestionBodyParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = Model.SubQuestionDetailsDto.Body,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                IsReadOnly = false
            };

            InstructionsParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.SubQuestionDetailsDto.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                IsReadOnly = Model.SubQuestionDetailsDto.IsUsedInExam
            };

            Model.SubQuestionDetailsDto.Choices.ForEach(choice =>
            {
                if (choice.ChoiceText == Resource.True || choice.ChoiceText.Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    TrueChoiceId = choice.Id;
                }
                else if (choice.ChoiceText == Resource.False || choice.ChoiceText.Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    FalseChoiceId = choice.Id;
                }

                if (choice.IsCorrectAnswer)
                {
                    SelectedOption = choice.ChoiceText == Resource.True || choice.ChoiceText.Equals("True", StringComparison.OrdinalIgnoreCase);
                }
            });

            StateHasChanged();
        }

        private async Task<bool> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            return !QuestionBodyParams.ErrorShown;
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

            Model.SubQuestionDetailsDto.Body = await QuestionBodyParams.GetTextEditorContentAsync();

            Model.SubQuestionDetailsDto.Choices.Clear();

            Model.SubQuestionDetailsDto.Choices.Add(new ChoiceDataDto
            {
                Id = TrueChoiceId,
                ChoiceText = Resource.True,
                IsCorrectAnswer = SelectedOption
            });

            Model.SubQuestionDetailsDto.Choices.Add(new ChoiceDataDto
            {
                Id = FalseChoiceId,
                ChoiceText = Resource.False,
                IsCorrectAnswer = !SelectedOption
            });

            Model.SubQuestionDetailsDto.ModelAnswer = SelectedOption ? Resource.True : Resource.False;

            Model.SubQuestionDetailsDto.Instructions = await InstructionsParams.GetTextEditorContentAsync();

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private void RemoveBodyMedia()
        {
            Model.SubQuestionDetailsDto.AttachmentFileName = null;
            StateHasChanged();
        }
    }
}
