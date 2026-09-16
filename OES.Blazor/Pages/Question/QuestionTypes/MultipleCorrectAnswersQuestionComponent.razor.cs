using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
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

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class MultipleCorrectAnswersQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams QuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private List<TextEditorParams> Choices { get; set; } = new();
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private IReadOnlyCollection<Guid> _correctAnswersGuids = [];


        protected override async Task OnInitializedAsync()
        {
            InitializeTextEditors();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();

            InitializeChoicesInCaseOfInsertion();
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

            List<Guid> _localCorrectAnswersGuids = [];

            Choices.Clear();

            Model.Choices.ForEach(choice =>
            {
                var newTextEditorParams = new TextEditorParams
                {
                    CustomId = choice.Id,
                    Label = Resource.ChoiceNumber + (Model.Choices.IndexOf(choice) + 1),
                    InitialContent = choice.ChoiceText,
                    WithMathChemPanel = true,
                    WithFileManagerPanel = true,
                    ErrorText = _requiredErrorText,
                    IsReadOnly = false,
                };

                if (choice.IsCorrectAnswer)
                    _localCorrectAnswersGuids.Add(newTextEditorParams.ComponentGuid);

                Choices.Add(newTextEditorParams);
            });

            _correctAnswersGuids = _localCorrectAnswersGuids;

            StateHasChanged();
        }

        private void InitializeChoicesInCaseOfInsertion()
        {
            if (Model.Id == 0)
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
            int choiceIndex = Choices.Count + 1;

            Choices.Add(new TextEditorParams
            {
                Label = Resource.ChoiceNumber + choiceIndex,
                WithMathChemPanel = false,
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
                { p => p.SubmitText, Resource.Yes  },
                { p => p.CancelText, Resource.Cancel  }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
            };

            var dialog = DialogService.Show<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await choice.DisposeJsEditorAsync();

                Choices.Remove(choice);

                ReorderChoices();

                _correctAnswersGuids = [];
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

            if (SelectedLanguage.Id == 0 || QuestionBodyParams.ErrorShown || Choices.Exists(c => c.ErrorShown))
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
            else if (_correctAnswersGuids.Count == 0)
            {
                return (false, Resource.CannotProceedWithNocorrectAnswerSelected);
            }
            else if (_correctAnswersGuids.Count == 1)
            {
                return (false, Resource.CannotProceedWithasingleCorrectAnswerSelectedNoteThatThisIsAnMCAQuestion);
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

            // Snapshot attachment file names before clearing
            var attachmentSnapshot = Model.Choices
                .Where(c => c.Id != 0 && !string.IsNullOrEmpty(c.AttachmentFileName))
                .ToDictionary(c => c.Id, c => c.AttachmentFileName);

            Model.Choices.Clear();

            foreach (var choice in Choices)
            {
                Model.Choices.Add(new ChoiceDataDto
                {
                    Id = choice.CustomId,
                    ChoiceText = await choice.GetTextEditorContentAsync(),
                    IsCorrectAnswer = _correctAnswersGuids.Contains(choice.ComponentGuid),
                    AttachmentFileName = attachmentSnapshot.GetValueOrDefault(choice.CustomId)
                });
            }

            var correctAnswers = Model.Choices
                .Where(choice => choice.IsCorrectAnswer)
                .Select(choice => choice.ChoiceText)
                .ToList();

            Model.ModelAnswer = string.Join(", ", correctAnswers);

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

        private async Task RemoveBodyMedia()
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisItem },
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
                Model.AttachmentFileName = null;
                StateHasChanged();
            }
        }

        private async Task RemoveChoiceMedia(long choiceId)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisItem },
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
                var choice = Model.Choices?.FirstOrDefault(c => c.Id == choiceId);

                if (choice != null)
                {
                    choice.AttachmentFileName = null;
                    StateHasChanged();
                }
            }
        }
    }
}
