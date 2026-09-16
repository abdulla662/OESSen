using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Question.QuestionTypes.ComprehensionComponents
{
    public partial class ComprehensionMCQQuestion
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public ComprehensionSubQuestionDto Model { get; set; }
        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public EventCallback<ComprehensionSubQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private List<TextEditorParams> Choices { get; set; } = new();

        private bool _processing = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private Guid _correctAnswerGuid = Guid.Empty;


        protected override void OnInitialized()
        {
            InitializeTextEditors();

            InitializeChoicesInCaseOfInsertion();
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

            Choices.Clear();

            var orderedChoices = Model.SubQuestionDetailsDto.Choices.OrderBy(c => c.OrderId).ToList();

            for (int i = 0; i < orderedChoices.Count; i++)
            {
                var choice = orderedChoices[i];
                var newTextEditorParams = new TextEditorParams
                {
                    CustomId = choice.Id,
                    Label = Resource.ChoiceNumber + (Model.SubQuestionDetailsDto.Choices.IndexOf(choice) + 1),
                    InitialContent = choice.ChoiceText,
                    WithMathChemPanel = true,
                    WithFileManagerPanel = true,
                    ErrorText = _requiredErrorText,
                    IsReadOnly = false,
                };

                if (choice.IsCorrectAnswer)
                    _correctAnswerGuid = newTextEditorParams.ComponentGuid;

                Choices.Add(newTextEditorParams);
            }

            StateHasChanged();
        }

        private void InitializeChoicesInCaseOfInsertion()
        {
            if (Model.SubQuestionDetailsDto.Id == 0)
            {
                Choices.Clear();

                for (int i = 0; i < 4; i++)
                {
                    AddChoice();
                }
            }
        }

        private void AddChoice()
        {
            var choiceIndex = Choices.Count + 1;

            Choices.Add(new TextEditorParams
            {
                Label = Resource.ChoiceNumber + choiceIndex,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText
            });
        }

        private async Task RemoveChoiceAsync(TextEditorParams choice)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisChoice },
                { p => p.SubmitText, Resource.Yes},
                { p => p.CancelText, Resource.Cancel}
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await choice.DisposeJsEditorAsync();

                Choices.Remove(choice);

                ReorderChoices();

                _correctAnswerGuid = Guid.Empty;
            }
        }

        private void ReorderChoices()
        {
            for (int i = 0; i < Choices.Count; i++)
            {
                Choices[i].Label = Resource.ChoiceNumber + (i + 1);
            }
        }

        private async Task<(bool IsValid, string Message)> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            var tasks = Choices.Select(async choice => choice.ErrorShown = string.IsNullOrWhiteSpace(await choice.GetTextEditorContentAsync()));

            await Task.WhenAll(tasks);

            if (QuestionBodyParams.ErrorShown || Choices.Exists(c => c.ErrorShown))
            {
                return (false, Resource.CannotProceedWithEmptyOrInvalidMandatoryFields);
            }
            else if (Choices.Count == 0)
            {
                return (false, Resource.CannotProceedWithNoEnteredChoices);
            }
            else if (Choices.Count < 2)
            {
                return (false, Resource.NumberOfChoicesCannotBeLessThanTwo);
            }
            else if (_correctAnswerGuid == Guid.Empty)
            {
                return (false, Resource.CannotProceedWithNocorrectAnswerSelected);
            }
            else
            {
                return (true, string.Empty);
            }
        }

        public async Task OnSubmitAsync()
        {
            _processing = true;

            var validationResult = await ValidateFormAsync();

            if (!validationResult.IsValid)
            {
                Snackbar.Add(validationResult.Message, Severity.Error);
                _processing = false;
                return;
            }

            Model.SubQuestionDetailsDto.Body = await QuestionBodyParams.GetTextEditorContentAsync();

            // Snapshot attachment file names before clearing
            var attachmentSnapshot = Model.SubQuestionDetailsDto.Choices
                .Where(c => c.Id != 0 && !string.IsNullOrEmpty(c.AttachmentFileName))
                .ToDictionary(c => c.Id, c => c.AttachmentFileName);

            Model.SubQuestionDetailsDto.Choices.Clear();

            int orderIndex = 1;
            foreach (var choice in Choices)
            {
                Model.SubQuestionDetailsDto.Choices.Add(new ChoiceDataDto
                {
                    Id = choice.CustomId,
                    ChoiceText = await choice.GetTextEditorContentAsync(),
                    IsCorrectAnswer = _correctAnswerGuid == choice.ComponentGuid,
                    OrderId = orderIndex++,
                    AttachmentFileName = attachmentSnapshot.GetValueOrDefault(choice.CustomId)
                });
            }

            var correctChoice = Choices.Find(choice => choice.ComponentGuid == _correctAnswerGuid);

            Model.SubQuestionDetailsDto.ModelAnswer = correctChoice is not null ? await correctChoice.GetTextEditorContentAsync() : string.Empty;

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

        private void RemoveChoiceMedia(long choiceId)
        {
            var choice = Model.SubQuestionDetailsDto.Choices?.FirstOrDefault(c => c.Id == choiceId);

            if (choice != null)
            {
                choice.AttachmentFileName = null;
                StateHasChanged();
            }
        }
    }
}
