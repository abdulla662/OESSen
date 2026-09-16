using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class OrderingQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

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

            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();

            InitializeSentencesInCaseOfInsertion();
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
                IsReadOnly = false
            };

            InstructionsParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                IsReadOnly = false
            };

            Sentences.Clear();

            for (int i = 0; i < Model.Choices.Count; i++)
            {
                var sentence = Model.Choices[i];
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
            if (Model.Id == 0)
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
                { p => p.Title, Resource.ConfirmDelete },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisSentence },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
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

            Model.LanguageId = SelectedLanguage.Id;

            Model.Body = await QuestionBodyParams.GetTextEditorContentAsync();

            Model.Choices.Clear();

            for (int i = 0; i < Sentences.Count; i++)
            {
                var sentence = Sentences[i];

                Model.Choices.Add(new ChoiceDataDto
                {
                    Id = sentence.CustomId,
                    ChoiceText = await sentence.GetTextEditorContentAsync(),
                    OrderId = i + 1
                });
            }

            var correctOrder = Model.Choices.ConvertAll(c => new OrderingQuestionAnswerDto
            {
                ChoiceId = c.Id,
                OrderId = c.OrderId
            });

            Model.ModelAnswer = JsonSerializer.Serialize(correctOrder);

            Model.Instructions = await InstructionsParams.GetTextEditorContentAsync();

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

        private void RemoveChoiceMedia(long choiceId)
        {
            var choice = Model.Choices?.FirstOrDefault(c => c.Id == choiceId);

            if (choice != null)
            {
                choice.AttachmentFileName = null;
                StateHasChanged();
            }
        }
    }
}
