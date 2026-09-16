using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes.ComprehensionComponents
{
    public partial class ComprehensionOrderingQuestion
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public long ParentMetadataId { get; set; }
        [Parameter] public ComprehensionSubQuestionDto Model { get; set; }
        [Parameter] public EventCallback<ComprehensionSubQuestionDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private List<TextEditorParams> Sentences { get; set; } = [];
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.SubQuestionDetailsDto.LanguageId) ?? new();

            InitializeSentencesInCaseOfInsertion();
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


            Sentences.Clear();

            for (int i = 0; i < Model.SubQuestionDetailsDto.Choices.Count; i++)
            {
                var sentence = Model.SubQuestionDetailsDto.Choices[i];
                var newTextEditorParams = new TextEditorParams
                {
                    CustomId = sentence.Id,
                    Label = Resource.SentenceNumber + (i + 1),
                    InitialContent = sentence.ChoiceText,
                    WithMathChemPanel = true,
                    WithFileManagerPanel = true,
                    ErrorText = _requiredErrorText,
                    IsReadOnly = false,
                };

                Sentences.Add(newTextEditorParams);
            }

            StateHasChanged();
        }

        private void InitializeSentencesInCaseOfInsertion()
        {
            if (Model.SubQuestionDetailsDto.Id == 0)
            {
                Sentences.Clear();

                for (int i = 0; i < 2; i++)
                {
                    AddSentence();
                }
            }
        }

        private void AddSentence()
        {
            int sentenceIndex = Sentences.Count + 1;

            Sentences.Add(new TextEditorParams
            {
                Label = Resource.SentenceNumber + sentenceIndex,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText
            });
        }

        private async Task RemoveSentenceAsync(TextEditorParams sentence)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisSentence },
                { p => p.SubmitText, Resource.Yes },
                { p => p.CancelText, Resource.Cancel }
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
                await sentence.DisposeJsEditorAsync();

                Sentences.Remove(sentence);

                ReorderSentences();
            }
        }

        private void ReorderSentences()
        {
            for (int i = 0; i < Sentences.Count; i++)
            {
                Sentences[i].Label = Resource.SentenceNumber + (i + 1);
            }
        }

        private void MoveSentenceUp(int index)
        {
            if (index <= 0 || index >= Sentences.Count) return;

            (Sentences[index], Sentences[index - 1]) = (Sentences[index - 1], Sentences[index]);

            ReorderSentences();
            StateHasChanged();
        }

        private void MoveSentenceDown(int index)
        {
            if (index < 0 || index >= Sentences.Count - 1) return;

            (Sentences[index], Sentences[index + 1]) = (Sentences[index + 1], Sentences[index]);

            ReorderSentences();
            StateHasChanged();
        }

        private async Task<(bool IsValid, string Message)> ValidateFormAsync()
        {
            QuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(await QuestionBodyParams.GetTextEditorContentAsync());

            var tasks = Sentences.Select(async sentence => sentence.ErrorShown = string.IsNullOrWhiteSpace(await sentence.GetTextEditorContentAsync()));

            await Task.WhenAll(tasks);

            if (SelectedLanguage.Id == 0 || QuestionBodyParams.ErrorShown || Sentences.Exists(c => c.ErrorShown))
            {
                return (false, Resource.CannotProceedWithEmptyOrInvalidMandatoryFields);
            }
            else if (Sentences.Count == 0)
            {
                return (false, Resource.CannotProceedWithNoEnteredSentences);
            }
            else if (Sentences.Count < 2)
            {
                return (false, Resource.NumberOfSentencesCannotBeLessThanTwo);
            }
            else
            {
                return (true, string.Empty);
            }
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

            Model.SubQuestionDetailsDto.LanguageId = SelectedLanguage.Id;

            Model.SubQuestionDetailsDto.Body = await QuestionBodyParams.GetTextEditorContentAsync();

            Model.SubQuestionDetailsDto.Choices.Clear();

            for (int i = 0; i < Sentences.Count; i++)
            {
                var sentence = Sentences[i];
                Model.SubQuestionDetailsDto.Choices.Add(new ChoiceDataDto
                {
                    Id = sentence.CustomId,
                    ChoiceText = await sentence.GetTextEditorContentAsync(),
                    OrderId = i + 1
                });
            }

            var correctOrder = Model.SubQuestionDetailsDto.Choices.ConvertAll(c => new OrderingQuestionAnswerDto
            {
                ChoiceId = c.Id,
                OrderId = c.OrderId
            });

            Model.SubQuestionDetailsDto.ModelAnswer = JsonSerializer.Serialize(correctOrder);

            Model.SubQuestionDetailsDto.Instructions = await InstructionsParams.GetTextEditorContentAsync();

            await FormHandler.InvokeAsync(Model);

            _processing = false;
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }
    }
}
