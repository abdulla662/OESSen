using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class MatchingPairsQuestionComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public long CreatedQuestionMetaDataId { get; set; }
        [Parameter] public GetMatchingPairsResponseDto AllMatchingPairsData { get; set; }
        [Parameter] public EventCallback<AddOrUpdateMatchingPairsRequestDto> MatchingFormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        private QuestionDetailsDto ParentQuestionDetails { get; set; } = new();
        public TextEditorParams ParentQuestionBodyParams { get; set; }
        public TextEditorParams InstructionsParams { get; set; }
        private List<TextEditorParams> MatchingPairsTextEditors { get; set; } = [];

        private Dictionary<Guid, string> MatchingPairsAnswers { get; set; } = [];
        private Dictionary<Guid, long> MatchingPairsItemIds { get; set; } = [];
        private Dictionary<Guid, string> MatchingPairsAttachmentFileNames { get; set; } = [];

        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];
        private List<MatchingPairQuestionItemDto> AnswerItems { get; set; } = [];
        private string NewAnswerText { get; set; } = string.Empty;

        private long _tempIdCounter = -1;
        private long? _editingAnswerId = null;
        private string _editingAnswerText = string.Empty;
        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;

        protected override async Task OnInitializedAsync()
        {
            if (AllMatchingPairsData?.QuestionDetails != null && AllMatchingPairsData.QuestionDetails.Any())
            {
                ParentQuestionDetails = AllMatchingPairsData.QuestionDetails.FirstOrDefault(q => q.QuestionMetadataId == AllMatchingPairsData.MetadataParentId) ?? new();
            }
            else
            {
                ParentQuestionDetails = new QuestionDetailsDto { QuestionMetadataId = CreatedQuestionMetaDataId };
            }

            LoadExistingData();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            SelectedLanguage = LanguagesList.FirstOrDefault(l => l.Id == ParentQuestionDetails.LanguageId) ?? new();

            StateHasChanged();
        }

        private void InitializeTextEditors()
        {
            ParentQuestionBodyParams = new()
            {
                Label = Resource.QuestionHeader,
                InitialContent = ParentQuestionDetails?.Body ?? string.Empty,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                IsReadOnly = false
            };

            InstructionsParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = ParentQuestionDetails?.Instructions ?? string.Empty,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                IsReadOnly = false
            };
        }

        private void LoadExistingData()
        {
            if (AllMatchingPairsData?.QuestionDetails == null || !AllMatchingPairsData.QuestionDetails.Any())
            {
                ResetData();

                InitializeTextEditors();

                return;
            }

            SelectedLanguage = LanguagesList.FirstOrDefault(l => l.Id == ParentQuestionDetails.LanguageId) ?? SelectedLanguage;

            ResetData();

            if (AllMatchingPairsData.MatchingItems != null && AllMatchingPairsData.MatchingItems.Count != 0)
            {
                var questionItems = AllMatchingPairsData.MatchingItems
                    .Where(i => i.IsDataSource)
                    .OrderBy(i => i.Id)
                    .ToList();

                AnswerItems = [.. AllMatchingPairsData.MatchingItems
                    .Where(i => !i.IsDataSource)
                    .OrderBy(i => i.Id)
                    .Select(i => new MatchingPairQuestionItemDto
                    {
                        Id = i.Id,
                        Body = i.Body,
                        ColumnOrder = 2,
                        IsDataSource = false
                    })];

                foreach (var questionItem in questionItems)
                {
                    var textEditor = new TextEditorParams
                    {
                        Label = Resource.QuestionTitle,
                        InitialContent = questionItem.Body,
                        WithMathChemPanel = true,
                        WithFileManagerPanel = true,
                        ErrorText = _requiredErrorText,
                        RequiredAsteriskShown = true,
                        IsReadOnly = false
                    };

                    MatchingPairsTextEditors.Add(textEditor);
                    MatchingPairsItemIds[textEditor.ComponentGuid] = questionItem.Id;
                    MatchingPairsAnswers[textEditor.ComponentGuid] = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(ParentQuestionDetails.ModelAnswer))
                {
                    try
                    {
                        var modelAnswers = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(ParentQuestionDetails.ModelAnswer);
                        if (modelAnswers != null)
                        {
                            var answerMap = AnswerItems.ToDictionary(a => a.Id, a => a.Body);
                            foreach (var modelAnswer in modelAnswers)
                            {
                                var editorGuid = MatchingPairsItemIds.FirstOrDefault(kvp => kvp.Value == modelAnswer.QuestionItemId).Key;
                                if (editorGuid != Guid.Empty && modelAnswer.AnswerIds != null && modelAnswer.AnswerIds.Count != 0)
                                {
                                    var firstAnswerId = modelAnswer.AnswerIds.First();
                                    if (answerMap.TryGetValue(firstAnswerId, out var answerText))
                                    {
                                        MatchingPairsAnswers[editorGuid] = answerText;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignored for now
                    }
                }
            }
            else
            {
                var legacyAnswers = AllMatchingPairsData.QuestionDetails
                    .Where(q => q.QuestionMetadataId != AllMatchingPairsData.MetadataParentId)
                    .SelectMany(q => q.Choices ?? [])
                    .Select(c => c.ChoiceText)
                    .Distinct()
                    .ToList();

                foreach (var ans in legacyAnswers)
                {
                    AnswerItems.Add(new()
                    {
                        Id = _tempIdCounter--,
                        Body = ans,
                        ColumnOrder = 2,
                        IsDataSource = false
                    });
                }

                var matchingPairs = AllMatchingPairsData
                    .QuestionDetails
                    .Where(q => q.QuestionMetadataId != AllMatchingPairsData.MetadataParentId)
                    .ToList();

                foreach (var matchingPair in matchingPairs)
                {
                    var textEditor = new TextEditorParams
                    {
                        Label = Resource.QuestionTitle,
                        InitialContent = matchingPair.Body,
                        WithMathChemPanel = true,
                        WithFileManagerPanel = true,
                        ErrorText = _requiredErrorText,
                        RequiredAsteriskShown = true,
                        IsReadOnly = false
                    };

                    MatchingPairsTextEditors.Add(textEditor);
                    MatchingPairsAnswers[textEditor.ComponentGuid] = matchingPair.ModelAnswer;
                    MatchingPairsItemIds[textEditor.ComponentGuid] = _tempIdCounter--;
                    MatchingPairsAttachmentFileNames[textEditor.ComponentGuid] = matchingPair.AttachmentFileName;
                }
            }

            InitializeTextEditors();

            StateHasChanged();
        }

        private void ResetData()
        {
            MatchingPairsTextEditors.Clear();
            MatchingPairsAnswers.Clear();
            MatchingPairsItemIds.Clear();
            MatchingPairsAttachmentFileNames.Clear();
            AnswerItems.Clear();
            _tempIdCounter = -1;
        }

        private void AddAnswer()
        {
            if (string.IsNullOrWhiteSpace(NewAnswerText))
                return;

            var trimmedAnswer = NewAnswerText.Trim();
            if (AnswerItems.Any(a => a.Body.Equals(trimmedAnswer, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.ThisAnswerAlreadyExists, Severity.Warning);
                return;
            }

            AnswerItems.Add(new()
            {
                Id = _tempIdCounter--,
                Body = trimmedAnswer,
                ColumnOrder = 2,
                IsDataSource = false
            });

            NewAnswerText = string.Empty;
        }

        private void RemoveAnswer(MatchingPairQuestionItemDto answer)
        {
            var isUsed = MatchingPairsAnswers.ContainsValue(answer.Body);

            if (isUsed)
            {
                Snackbar.Add(Resource.CannotDeleteThisAnswerItIsBeingUsedInAQuestion, Severity.Error);
                return;
            }

            AnswerItems.Remove(answer);
        }

        private void AddMatchingPair()
        {
            var newEditorParams = new TextEditorParams
            {
                Label = Resource.QuestionTitle,
                InitialContent = string.Empty,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true
            };

            var tempId = _tempIdCounter--;
            MatchingPairsTextEditors.Add(newEditorParams);
            MatchingPairsAnswers[newEditorParams.ComponentGuid] = string.Empty;
            MatchingPairsItemIds[newEditorParams.ComponentGuid] = tempId;
            MatchingPairsAttachmentFileNames[newEditorParams.ComponentGuid] = null;

            StateHasChanged();
        }

        private void RemoveMatchingPair(Guid editorGuid)
        {
            var editor = MatchingPairsTextEditors.FirstOrDefault(e => e.ComponentGuid == editorGuid);
            if (editor == null) return;

            MatchingPairsTextEditors.Remove(editor);
            MatchingPairsAnswers.Remove(editorGuid);
            MatchingPairsItemIds.Remove(editorGuid);
            MatchingPairsAttachmentFileNames.Remove(editorGuid);

            Snackbar.Add(Resource.SelectedQuestionsRemoved, Severity.Info);
            StateHasChanged();
        }

        private void RemoveBodyMedia()
        {
            ParentQuestionDetails.AttachmentFileName = null;
            StateHasChanged();
        }

        private void RemoveMatchingPairMedia(Guid editorGuid)
        {
            MatchingPairsAttachmentFileNames[editorGuid] = null;
            StateHasChanged();
        }

        private List<MatchingPairQuestionItemDto> GetAvailableAnswersForMatchingPair(Guid currentEditorGuid)
        {
            var usedAnswers = MatchingPairsAnswers
                .Where(kvp => kvp.Key != currentEditorGuid && !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(kvp => kvp.Value)
                .ToList();

            return [.. AnswerItems.Where(answer => !usedAnswers.Contains(answer.Body))];
        }

        private async Task<(bool IsValid, string Message)> ValidateFormAsync()
        {
            var parentBody = await ParentQuestionBodyParams.GetTextEditorContentAsync();

            ParentQuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(parentBody);

            if (SelectedLanguage.Id == 0 || ParentQuestionBodyParams.ErrorShown)
            {
                return (false, Resource.CannotProceedWithEmptyOrInvalidMandatoryFields);
            }

            if (AnswerItems.Count < 2)
            {
                return (false, Resource.PleaseAddAtLeastTwoAndwersForMatching);
            }

            if (MatchingPairsTextEditors.Count < 2)
            {
                return (false, Resource.MatchingPairsMinimumTwo);
            }

            if (AnswerItems.Count != MatchingPairsTextEditors.Count)
            {
                return (false, Resource.TheNumberOfAnswersShouldMatchTheNumberOfQuestions);
            }

            foreach (var editor in MatchingPairsTextEditors)
            {
                var editorContent = await editor.GetTextEditorContentAsync();
                if (string.IsNullOrWhiteSpace(editorContent))
                {
                    editor.ErrorShown = true;
                    return (false, Resource.CannotProceedWithEmptyOrInvalidMandatoryFields);
                }
                editor.ErrorShown = false;
            }

            foreach (var kvp in MatchingPairsAnswers)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    return (false, Resource.PleaseSelectCorrectAnswer);
                }
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

            ParentQuestionDetails.LanguageId = SelectedLanguage.Id;
            ParentQuestionDetails.Body = await ParentQuestionBodyParams.GetTextEditorContentAsync();
            ParentQuestionDetails.Instructions = await InstructionsParams.GetTextEditorContentAsync();

            var questionItems = new List<MatchingPairQuestionItemDto>();
            foreach (var editor in MatchingPairsTextEditors)
            {
                var questionBody = await editor.GetTextEditorContentAsync();
                if (!MatchingPairsItemIds.TryGetValue(editor.ComponentGuid, out var questionId))
                {
                    questionId = _tempIdCounter--;
                    MatchingPairsItemIds[editor.ComponentGuid] = questionId;
                }

                questionItems.Add(new()
                {
                    Id = questionId,
                    Body = questionBody,
                    ColumnOrder = 1,
                    IsDataSource = true
                });
            }

            var modelAnswers = new List<MatchingPairModelAnswerDto>();
            foreach (var editor in MatchingPairsTextEditors)
            {
                var qId = MatchingPairsItemIds.TryGetValue(editor.ComponentGuid, out var mappedQId) ? mappedQId : 0;
                var selectedAnswer = MatchingPairsAnswers.TryGetValue(editor.ComponentGuid, out var selAns) ? selAns : null;
                var matchedAnswer = AnswerItems.FirstOrDefault(a => a.Body == selectedAnswer);

                if (matchedAnswer != null && qId != 0)
                {
                    modelAnswers.Add(new()
                    {
                        QuestionItemId = qId,
                        AnswerIds = [matchedAnswer.Id]
                    });
                }
            }

            ParentQuestionDetails.ModelAnswer = JsonSerializer.Serialize(modelAnswers);

            var allMatchingItems = new List<MatchingPairQuestionItemDto>();
            allMatchingItems.AddRange(questionItems);
            allMatchingItems.AddRange(AnswerItems);

            var questionDetailsList = new List<QuestionDetailsDto>
            {
                new()
                {
                    Id = ParentQuestionDetails.Id,
                    QuestionMetadataId = CreatedQuestionMetaDataId,
                    LanguageId = SelectedLanguage.Id,
                    Body = ParentQuestionDetails.Body,
                    Instructions = ParentQuestionDetails.Instructions,
                    ModelAnswer = ParentQuestionDetails.ModelAnswer,
                    UseArabicNumbers = ParentQuestionDetails.UseArabicNumbers,
                    HasShuffled = ParentQuestionDetails.HasShuffled,
                    AttachmentFileName = ParentQuestionDetails.AttachmentFileName,
                    Choices = []
                }
            };

            var matchingDto = new AddOrUpdateMatchingPairsRequestDto(
                CreatedQuestionMetaDataId,
                questionDetailsList,
                allMatchingItems
            );

            if (MatchingFormHandler.HasDelegate)
            {
                await MatchingFormHandler.InvokeAsync(matchingDto);
            }
            else
            {
                Snackbar.Add(Resource.ErrorSavingQuestion, Severity.Error);
            }

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
                ParentQuestionDetails.Instructions = selectedTemplate.Content;

                if (InstructionsParams.TextEditor != null)
                {
                    await InstructionsParams.TextEditor.SetTextEditorContentAsync(selectedTemplate.Content);
                }

                Snackbar.Add(Resource.TemplateFetchedSuccessfully, Severity.Success);
                StateHasChanged();
            }
        }

        private void StartEditAnswer(MatchingPairQuestionItemDto answer)
        {
            _editingAnswerId = answer.Id;
            _editingAnswerText = answer.Body;
        }

        private void CancelEditAnswer()
        {
            _editingAnswerId = null;
            _editingAnswerText = string.Empty;
        }

        private void SaveEditAnswer()
        {
            if (string.IsNullOrWhiteSpace(_editingAnswerText) || _editingAnswerId == null)
                return;

            var trimmed = _editingAnswerText.Trim();
            var item = AnswerItems.FirstOrDefault(a => a.Id == _editingAnswerId.Value);
            if (item == null)
            {
                CancelEditAnswer();
                return;
            }

            if (trimmed == item.Body)
            {
                CancelEditAnswer();
                return;
            }

            if (AnswerItems.Any(a => a.Id != item.Id && a.Body.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.ThisAnswerAlreadyExists, Severity.Warning);
                return;
            }

            var oldText = item.Body;
            item.Body = trimmed;

            var keysToUpdate = MatchingPairsAnswers
               .Where(kvp => kvp.Value == oldText)
               .Select(kvp => kvp.Key)
               .ToList();

            foreach (var key in keysToUpdate)
                MatchingPairsAnswers[key] = trimmed;

            CancelEditAnswer();
            StateHasChanged();
        }
    }
}