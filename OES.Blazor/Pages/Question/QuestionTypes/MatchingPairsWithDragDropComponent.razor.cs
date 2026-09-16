using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class MatchingPairsWithDragDropComponent
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public long CreatedQuestionMetaDataId { get; set; }
        [Parameter] public GetMatchingPairsWithDragDropResponseDto InitialData { get; set; }
        [Parameter] public EventCallback<AddOrUpdateMatchingPairsWithDragDropRequestDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        private QuestionDetailsDto ParentQuestionDetails { get; set; } = new();
        private TextEditorParams ParentQuestionBodyParams { get; set; }
        private TextEditorParams InstructionsParams { get; set; }

        private List<MatchingPairQuestionItemDto> QuestionItems { get; set; } = [];
        private List<MatchingPairQuestionItemDto> AnswerItems { get; set; } = [];
        private List<MatchingPairQuestionItemDto> DropdownQuestionItems { get; set; } = [];

        private Dictionary<long, TextEditorParams> EditorParamsDict { get; set; } = new();
        private Dictionary<long, MatchingPairQuestionItemDto> CorrectAnswerDict { get; set; } = new();

        private long _tempIdCounter = -1;
        private long _openDropdownId = 0;
        private long _hoveredId = 0;
        private long _hoveredTriggerId = 0;

        private List<LanguageDto> LanguagesList { get; set; } = [];
        private LanguageDto SelectedLanguage { get; set; } = new();

        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private bool _isAnswersEnabled = false;

        protected override async Task OnInitializedAsync()
        {
            ParentQuestionDetails = InitialData?.QuestionDetails.FirstOrDefault(q => q.QuestionMetadataId == InitialData.MetadataParentId) ?? new QuestionDetailsDto { QuestionMetadataId = CreatedQuestionMetaDataId };

            InitializeTextEditors();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();

            if (InitialData != null)
            {
                SelectedLanguage = LanguagesList.FirstOrDefault(l => l.Id == ParentQuestionDetails.LanguageId) ?? new();

                var allItems = InitialData.MatchingItems.Select(i => new MatchingPairQuestionItemDto
                {
                    Id = i.Id,
                    Body = i.Body,
                    IsDataSource = i.IsDataSource,
                    ColumnOrder = i.ColumnOrder
                }).ToList();

                foreach (var item in allItems)
                {
                    EditorParamsDict[item.Id] = CreateItemEditorParams(item.Body);
                    if (!item.IsDataSource)
                    {
                        CorrectAnswerDict[item.Id] = null;
                    }
                }

                QuestionItems = [.. allItems.Where(i => i.IsDataSource)];
                DropdownQuestionItems = [.. QuestionItems];
                AnswerItems = [.. allItems.Where(i => !i.IsDataSource)];

                var referenceList = new List<MatchingPairQuestionItemDto>();
                referenceList.AddRange(QuestionItems);
                referenceList.AddRange(AnswerItems);

                RestoreMapping(ParentQuestionDetails.ModelAnswer, referenceList);
                _isAnswersEnabled = true;
                StateHasChanged();
            }
        }

        private TextEditorParams CreateItemEditorParams(string content = "")
        {
            return new TextEditorParams
            {
                InitialContent = content,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                Label = string.Empty
            };
        }

        private void InitializeTextEditors()
        {
            ParentQuestionBodyParams = new()
            {
                Label = Resource.QuestionHeader,
                InitialContent = ParentQuestionDetails?.Body ?? string.Empty,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                RequiredAsteriskShown = true
            };

            InstructionsParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = ParentQuestionDetails?.Instructions ?? string.Empty,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
            };
        }

        private void AddQuestion()
        {
            var id = _tempIdCounter--;
            QuestionItems.Add(new MatchingPairQuestionItemDto { Id = id, IsDataSource = true });
            EditorParamsDict[id] = CreateItemEditorParams();
        }

        private void AddAnswer()
        {
            var id = _tempIdCounter--;
            AnswerItems.Add(new MatchingPairQuestionItemDto { Id = id, IsDataSource = false });
            EditorParamsDict[id] = CreateItemEditorParams();
            CorrectAnswerDict[id] = null;
        }

        private void RemoveQuestion(MatchingPairQuestionItemDto item)
        {
            var linkedAnswers = CorrectAnswerDict
                .Where(kvp => kvp.Value?.Id == item.Id)
                .ToList();

            if (linkedAnswers.Count > 0)
            {
                Snackbar.Add(Resource.CannotDeleteQuestionLinkedToAnswer, Severity.Warning);
                return;
            }

            QuestionItems.Remove(item);
            DropdownQuestionItems.Remove(item);
            EditorParamsDict.Remove(item.Id);
        }

        private void RemoveAnswer(MatchingPairQuestionItemDto item)
        {
            AnswerItems.Remove(item);
            EditorParamsDict.Remove(item.Id);
            CorrectAnswerDict.Remove(item.Id);
        }

        private async Task AddQuestionsTextAsync()
        {
            if (!QuestionItems.Any())
            {
                Snackbar.Add(Resource.NoQuestionsToAdd, Severity.Warning);
                return;
            }

            if (QuestionItems.Count < 2)
            {
                Snackbar.Add(Resource.AddAtLeastTwoQuestions, Severity.Warning);
                return;
            }

            _processing = true;

            foreach (var q in QuestionItems)
            {
                if (EditorParamsDict.TryGetValue(q.Id, out var p) && p.TextEditor != null)
                {
                    q.Body = await p.GetTextEditorContentAsync();
                }
            }

            if (QuestionItems.Any(q => string.IsNullOrWhiteSpace(GetPlainText(q.Body))))
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            var newQuestions = QuestionItems
                .Where(q => !DropdownQuestionItems.Any(d => d.Id == q.Id))
                .ToList();

            var changedQuestions = QuestionItems
                .Where(q => DropdownQuestionItems.Any(d => d.Id == q.Id && d.Body != q.Body))
                .ToList();

            if (!newQuestions.Any() && !changedQuestions.Any())
            {
                Snackbar.Add(Resource.NoQuestionsToAdd, Severity.Warning);
                _processing = false;
                return;
            }

            foreach (var q in newQuestions)
                DropdownQuestionItems.Add(q);

            foreach (var q in changedQuestions)
            {
                var existing = DropdownQuestionItems.FirstOrDefault(d => d.Id == q.Id);
                if (existing != null)
                    existing.Body = q.Body;

                foreach (var kvp in CorrectAnswerDict.Where(kvp => kvp.Value?.Id == q.Id))
                    kvp.Value.Body = q.Body;
            }

            _processing = false;
            _isAnswersEnabled = true;
            Snackbar.Add(Resource.AllQuestionsAddedToMenu, Severity.Success);
            StateHasChanged();
        }

        private void RestoreMapping(string modelAnswerJson, List<MatchingPairQuestionItemDto> allItems)
        {
            if (string.IsNullOrWhiteSpace(modelAnswerJson)) return;

            var mapping = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(modelAnswerJson);
            if (mapping == null) return;

            foreach (var item in mapping)
            {
                var questionItem = allItems.FirstOrDefault(i => i.Id == item.QuestionItemId);
                if (questionItem == null) continue;

                foreach (var answerId in item.AnswerIds)
                {
                    var answerItem = allItems.FirstOrDefault(i => i.Id == answerId);
                    if (answerItem != null)
                    {
                        CorrectAnswerDict[answerItem.Id] = questionItem;
                    }
                }
            }
        }

        private string GetPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private void ToggleDropdown(long answerId)
        {
            _openDropdownId = _openDropdownId == answerId ? 0 : answerId;
            StateHasChanged();
        }

        private void SelectQuestion(long answerId, MatchingPairQuestionItemDto question)
        {
            CorrectAnswerDict[answerId] = question;
            _openDropdownId = 0;
            StateHasChanged();
        }

        private async Task OnSaveAsync()
        {
            _isSubmitButtonHit = true;
            _processing = true;

            foreach (var q in QuestionItems)
            {
                q.ColumnOrder = 1;
                if (EditorParamsDict.TryGetValue(q.Id, out var p) && p.TextEditor != null)
                {
                    q.Body = await p.GetTextEditorContentAsync();
                }
            }

            foreach (var a in AnswerItems)
            {
                a.ColumnOrder = 2;
                if (EditorParamsDict.TryGetValue(a.Id, out var p) && p.TextEditor != null)
                {
                    a.Body = await p.GetTextEditorContentAsync();
                }
            }

            var parentBody = await ParentQuestionBodyParams.GetTextEditorContentAsync();
            ParentQuestionBodyParams.ErrorShown = string.IsNullOrWhiteSpace(parentBody);

            foreach (var q in QuestionItems)
            {
                if (EditorParamsDict.TryGetValue(q.Id, out var p))
                    p.ErrorShown = string.IsNullOrWhiteSpace(GetPlainText(q.Body));
            }

            foreach (var a in AnswerItems)
            {
                if (EditorParamsDict.TryGetValue(a.Id, out var p))
                    p.ErrorShown = string.IsNullOrWhiteSpace(GetPlainText(a.Body));
            }

            if (!Validate())
            {
                _processing = false;
                return;
            }

            ParentQuestionBodyParams.ErrorShown = false;
            foreach (var p in EditorParamsDict.Values)
                p.ErrorShown = false;

            var allItems = new List<MatchingPairQuestionItemDto>();
            allItems.AddRange(QuestionItems);
            allItems.AddRange(AnswerItems);

            var modelAnswer = BuildModelAnswer(allItems);

            var request = new AddOrUpdateMatchingPairsWithDragDropRequestDto(
                CreatedQuestionMetaDataId,
                new List<QuestionDetailsDto>
                {
                    new QuestionDetailsDto
                    {
                        Id = ParentQuestionDetails.Id,
                        QuestionMetadataId = CreatedQuestionMetaDataId,
                        LanguageId = SelectedLanguage.Id,
                        Body = parentBody,
                        Instructions = await InstructionsParams.GetTextEditorContentAsync(),
                        ModelAnswer = modelAnswer,
                        UseArabicNumbers = ParentQuestionDetails.UseArabicNumbers,
                        HasShuffled = ParentQuestionDetails.HasShuffled
                    }
                },
                allItems
            );

            if (FormHandler.HasDelegate)
            {
                await FormHandler.InvokeAsync(request);
            }

            _processing = false;
        }

        private string BuildModelAnswer(List<MatchingPairQuestionItemDto> allItems)
        {
            var modelAnswers = new List<MatchingPairModelAnswerDto>();

            foreach (var question in QuestionItems)
            {
                var answerIds = CorrectAnswerDict
                    .Where(kvp => kvp.Value?.Id == question.Id)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (answerIds.Any())
                {
                    modelAnswers.Add(new MatchingPairModelAnswerDto
                    {
                        QuestionItemId = question.Id,
                        AnswerIds = answerIds
                    });
                }
            }

            return JsonSerializer.Serialize(modelAnswers);
        }

        private bool Validate()
        {
            if (SelectedLanguage.Id == 0)
            {
                Snackbar.Add(Resource.SelectLanguage, Severity.Error);
                return false;
            }

            if (ParentQuestionBodyParams.ErrorShown)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return false;
            }

            if (!QuestionItems.Any())
            {
                Snackbar.Add(Resource.MatchingPairsMinimumTwo, Severity.Error);
                return false;
            }

            if (!AnswerItems.Any())
            {
                Snackbar.Add(Resource.PleaseAddAtLeastTwoAndwersForMatching, Severity.Error);
                return false;
            }

            if (QuestionItems.Count > AnswerItems.Count)
            {
                Snackbar.Add(Resource.AnswersMustBeGreaterThanOrEqualQuestions, Severity.Error);
                return false;
            }

            var confirmedQuestionIds = DropdownQuestionItems.Select(q => q.Id).ToHashSet();
            if (QuestionItems.Any(q => !confirmedQuestionIds.Contains(q.Id)))
            {
                Snackbar.Add(Resource.AllQuestionsMustBeAddedToMenu, Severity.Error);
                return false;
            }

            var linkedQuestionIds = CorrectAnswerDict
                .Where(kvp => kvp.Value != null)
                .Select(kvp => kvp.Value.Id)
                .ToHashSet();

            if (QuestionItems.Any(q => !linkedQuestionIds.Contains(q.Id)))
            {
                Snackbar.Add(Resource.AllQuestionsMustBeLinkedToAtLeastOneAnswer, Severity.Error);
                return false;
            }

            if (QuestionItems.Any(q => string.IsNullOrWhiteSpace(GetPlainText(q.Body))))
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return false;
            }

            if (AnswerItems.Any(a => string.IsNullOrWhiteSpace(GetPlainText(a.Body))))
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return false;
            }

            return true;
        }

        private async Task SaveInstructionAsTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.SaveAsInstructionTemplate, parameters, options);
            var result = await dialog.Result;
            if (result.Canceled) return;
            var templateName = result.Data?.ToString();
            if (string.IsNullOrWhiteSpace(templateName)) { Snackbar.Add(Resource.NameRequired, Severity.Error); return; }
            var instructionsContent = await InstructionsParams.GetTextEditorContentAsync();
            if (string.IsNullOrWhiteSpace(instructionsContent)) { Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error); return; }
            var dto = new QuestionInstructionTemplateDto { Name = templateName, Content = instructionsContent };
            var response = await BlazInstructionTemplateService.SaveInstructionTemplateAsync(dto);
            if (response.CustomCodeStatus == CustomCodeStatus.Success) Snackbar.Add(response.Message, Severity.Success);
            else Snackbar.Add(response.Message, Severity.Error);
        }

        private async Task LoadInstructionTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await DialogService.ShowAsync<InstructionTemplateDialog>(Resource.SelectTemplate, parameters, options);
            var result = await dialog.Result;
            if (!result.Canceled && result.Data is QuestionInstructionTemplateDto selectedTemplate)
            {
                ParentQuestionDetails.Instructions = selectedTemplate.Content;
                if (InstructionsParams.TextEditor != null) await InstructionsParams.TextEditor.SetTextEditorContentAsync(selectedTemplate.Content);
                Snackbar.Add(Resource.TemplateFetchedSuccessfully, Severity.Success);
                StateHasChanged();
            }
        }

        private void RemoveBodyMedia()
        {
            ParentQuestionDetails.AttachmentFileName = null;
            StateHasChanged();
        }
    }
}