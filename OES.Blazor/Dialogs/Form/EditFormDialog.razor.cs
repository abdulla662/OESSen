using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.QuestionWithForms;
using OES.Blazor.Services.Interfaces.DifficultyLevel;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.MarkingScheme;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Form
{
    public partial class EditFormDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazFormService BlazFormService { get; set; }
        [Inject] private IBlazPaperService BlazPaperService { get; set; }
        [Inject] private IBlazDifficultyLevelService BlazDifficulyLevelService { get; set; }
        [Inject] private IBLazQuestionType BLazQuestionType { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IBlazMarkingSchemeService BlazMarkingSchemeService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;
        [Parameter] public long FormId { get; set; }
        [Parameter] public long PaperId { get; set; }

        private GetPaperMetadataResponseDto RelatedPaper { get; set; } = new();
        public ItemBanksFromItemBankPointResponseDto SelectedItemBank
        {
            get => _selectedItemBank;
            set
            {
                if (_selectedItemBank != value)
                {
                    _selectedItemBank = value;
                    questionFilterPaginationModel._selectedItemBank = _selectedItemBank?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }
        public DifficultyLevelDto SelectedDifficultyLevel
        {
            get => _selectedDifficultyLevel;
            set
            {
                if (_selectedDifficultyLevel != value)
                {
                    _selectedDifficultyLevel = value;
                    questionFilterPaginationModel._selectedDifficultyLevel = _selectedDifficultyLevel?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }
        public QuestionTypeDto SelectedType
        {
            get => _selectedType;
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    questionFilterPaginationModel._selectedType = _selectedType?.Id ?? 0;
                    _questionsListChangingKey += 1;
                }
            }
        }

        private readonly EditFormDto model = new();
        private HashSet<FormQuestionDetailedDto> _tableSelectedItems = [];
        private HashSet<FormQuestionDetailedDto> _selectedQuestions = [];
        private List<FormQuestionDetailedDto> usedQuestion = [];
        private readonly QuestionFilterPaginationModel questionFilterPaginationModel = new();
        private ItemBanksFromItemBankPointResponseDto _selectedItemBank;
        private DifficultyLevelDto _selectedDifficultyLevel;
        private QuestionTypeDto _selectedType;
        private List<ItemBanksFromItemBankPointResponseDto> _itemBanks = [];
        private List<DifficultyLevelDto> _difficultyLevels = [];
        private List<QuestionTypeDto> _questionTypes = [];
        private int _questionsListChangingKey;
        private string _selectedQuestionsSearchString = string.Empty;
        private readonly List<FormQuestionDetailedDto> _pendingReplacementQuestions = [];

        private readonly Dictionary<long, (long OriginalHangedId, double OriginalScore)> _undoReplacementMap = [];
        private readonly List<GetDifficultyLevelMarkingSchemeResponseDto> difficultyLevels = [];
        private readonly List<GetItemBanksMarkingSchemeResponseDto> itemBanks = [];
        private ScoreSchemaType _selectedScheme;
        private double currentTotal = 0;
        private double currentDifficultyLevelTotal = 0;
        private double currentItemBankTotal = 0;
        private bool _isAutoOrManualValid = true;
        private bool _isDifficultyLevelValid = true;
        private bool _isItemBankValid = true;
        private bool _isWeightedExamValid = true;
        private bool _pendingHangMapping = false;
        private long _lastHangedQuestionId = 0;
        private double _pendingHangOriginalScore = 0;
        private HashSet<long> _originalQuestionIds = [];


        protected override async Task OnInitializedAsync()
        {
            var formQuestionsTask = BlazFormService.GetAllQuestionByFormIdAsync(FormId);
            var relatedPaperMetadataTask = BlazPaperService.GetPaperMetaDataAsync(PaperId);
            var questionTypesTask = BLazQuestionType.GetAllQuestionType();
            var itemBanksTask = BlazPaperService.GetAllItemBanksFromItemBankPointsAsync(PaperId);
            var paperMarkingSchemeTask = BlazMarkingSchemeService.GetPaperMarkingSchemeAsync(PaperId);

            await Task.WhenAll(formQuestionsTask, relatedPaperMetadataTask, questionTypesTask, itemBanksTask, paperMarkingSchemeTask);

            var questionsList = await formQuestionsTask ?? [];
            _selectedQuestions = [.. questionsList];
            _originalQuestionIds = [.. _selectedQuestions.Select(q => q.QuestionId)];

            var relatedPaperMetadataResponse = await relatedPaperMetadataTask;
            var paperMarkingScheme = await paperMarkingSchemeTask;
            _questionTypes = await questionTypesTask ?? [];
            _itemBanks = await itemBanksTask ?? [];
            usedQuestion = [.. _selectedQuestions.Where(q => q.PaperQuestionStatus == PaperQuestionStatus.Used)];

            var firstQuestionInList = _selectedQuestions.FirstOrDefault();
            model.Name = firstQuestionInList?.FormName;
            model.Description = firstQuestionInList?.FormDescription;
            model.Code = firstQuestionInList?.FormCode;

            if (relatedPaperMetadataResponse.StatusCode == HttpStatusCode.OK)
            {
                RelatedPaper = (GetPaperMetadataResponseDto)relatedPaperMetadataResponse.Data;
            }
            else
            {
                RelatedPaper = new GetPaperMetadataResponseDto();
            }

            _difficultyLevels = await BlazDifficulyLevelService.GetDifficultyLevelByProfileIdAsync(RelatedPaper.DifficultyProfileId);
            _selectedScheme = paperMarkingScheme.ScoreType;

            InitializeAutoDifficultyLevel();

            InitializeAutoItemBanks();

            StateHasChanged();
        }

        private bool FilterSelectedQuestions(FormQuestionDetailedDto question)
        {
            if (string.IsNullOrWhiteSpace(_selectedQuestionsSearchString)) return true;

            return question.Code?.Contains(_selectedQuestionsSearchString, StringComparison.OrdinalIgnoreCase) == true;
        }


        #region Marking Scheme Part

        private void InitializeAutoDifficultyLevel()
        {
            difficultyLevels.Clear();

            var manualGroups = usedQuestion
                .GroupBy(q => new { q.DifficultyLevelId, q.DifficultyLevelName })
                .Select(g => new GetDifficultyLevelMarkingSchemeResponseDto
                {
                    DifficultyLevelId = g.Key.DifficultyLevelId,
                    DifficultyLevelName = g.Key.DifficultyLevelName,
                    SelectedCount = g.Count(),
                    SubQuestionCount = g.Sum(q => q.SubQuestionsCount),
                    TotalCount = g.Sum(x => x.SubQuestionsCount),
                    MarkPerQuestion = Math.Round(g.Average(x => (x.Score / x.SubQuestionsCount) ?? 0), 3)
                });

            difficultyLevels.AddRange(manualGroups);
        }

        private void InitializeAutoItemBanks()
        {
            itemBanks.Clear();

            var itemBankGroups = usedQuestion
                .GroupBy(q => new { q.ItembankId, q.ItemBankName })
                .Select(g =>
                {
                    var questionCount = g.Sum(x => x.SubQuestionsCount);

                    var totalScore = g.Sum(x => x.Score ?? 0);

                    return new GetItemBanksMarkingSchemeResponseDto
                    {
                        ItemBankId = g.Key.ItembankId,
                        ItemBankName = g.Key.ItemBankName,
                        QuestionCount = questionCount,
                        QuestionMark = questionCount == 0 ? 0 : Math.Round(totalScore / questionCount, 3),
                        ItemBankMark = totalScore
                    };
                });

            itemBanks.AddRange(itemBankGroups);
        }

        private void RecalculateAndValidateMarksAutoOrManual()
        {
            currentTotal = Math.Round(usedQuestion.Sum(q => q.Score ?? 0), 3);

            _isAutoOrManualValid = currentTotal == RelatedPaper.TotalMarks;

            StateHasChanged();
        }

        private void RecalculateAndValidateMarksDifficultyLevel()
        {
            currentDifficultyLevelTotal = difficultyLevels.Sum(d => d.MarkPerQuestion * d.TotalCount);

            _isDifficultyLevelValid = currentDifficultyLevelTotal == RelatedPaper.TotalMarks;

            StateHasChanged();
        }

        private void RecalculateAndValidateMarksItemBank()
        {
            currentItemBankTotal = itemBanks.Sum(b => b.ItemBankMark);

            _isItemBankValid = currentItemBankTotal == RelatedPaper.TotalMarks;

            if (_isItemBankValid)
            {
                foreach (var bank in itemBanks.Where(b => b.QuestionCount > 0))
                {
                    bank.QuestionMark = bank.ItemBankMark / bank.QuestionCount;
                }
            }

            StateHasChanged();
        }

        private void RecalculateAndValidateMarksWeightedExam()
        {
            if (RelatedPaper.QuestionSelectionType == QuestionSelectionType.Auto)
                _isWeightedExamValid = _selectedQuestions.All(question => question.Deltavalue >= 0.0 && question.Deltavalue <= 1.0);
            else if (RelatedPaper.QuestionSelectionType == QuestionSelectionType.Manual)
                _isWeightedExamValid = _selectedQuestions.All(question => question.Deltavalue >= 0.0 && question.Deltavalue <= 1.0);

            StateHasChanged();
        }

        private void RedistributeScoresIfNeeded()
        {
            if (RelatedPaper == null) return;

            switch (_selectedScheme)
            {
                case ScoreSchemaType.EqualDistribution:
                    RedistributeForManual();
                    break;
                case ScoreSchemaType.DifficultyLevelBasedDistribution:
                    RedistributeForDifficultyLevels();
                    break;
                case ScoreSchemaType.ItemBankBasedDistribution:
                    RedistributeForItemBanks();
                    break;
                default:
                    RedistributeForManual();
                    break;
            }

            RecalculateAndValidateMarksAutoOrManual();
            RecalculateAndValidateMarksDifficultyLevel();
            RecalculateAndValidateMarksItemBank();
        }

        private void RedistributeForManual()
        {
            var totalMarks = RelatedPaper.TotalMarks;
            var assignedMarks = usedQuestion.Sum(q => q.Score ?? 0);
            var unscoredQuestions = usedQuestion.Where(q => q.Score is null or 0).ToList();

            if (unscoredQuestions.Count == 0) return;

            var remainingMarks = totalMarks - assignedMarks;

            if (remainingMarks <= 0)
            {
                foreach (var q in unscoredQuestions) q.Score = 0;
                return;
            }

            var totalUnits = unscoredQuestions.Sum(q => q.SubQuestionsCount);

            var markPerUnit = Math.Round(remainingMarks / totalUnits, 3);

            double distributed = 0;

            for (int i = 0; i < unscoredQuestions.Count; i++)
            {
                var q = unscoredQuestions[i];

                var questionTotal = markPerUnit * q.SubQuestionsCount;

                q.Score = Math.Round(questionTotal, 3);

                distributed += q.Score ?? 0;
            }

            var roundingDiff = Math.Round(remainingMarks - distributed, 3);

            if (Math.Abs(roundingDiff) > 0 && unscoredQuestions.Count > 0)
            {
                unscoredQuestions[0].Score = Math.Round((unscoredQuestions[0].Score ?? 0) + roundingDiff, 3);
            }
        }

        private void RedistributeForDifficultyLevels()
        {
            var totalMarks = RelatedPaper.TotalMarks;

            var levels = difficultyLevels.ConvertAll(d => new
            {
                d.DifficultyLevelId,
                d.SelectedCount,
                d.SubQuestionCount,
                d.TotalCount,
                d.MarkPerQuestion
            });

            var totalSelectedQuestions = levels.Sum(l => l.TotalCount);
            if (totalSelectedQuestions == 0) return;

            double assignedMarksFromLevels = 0;
            int unassignedQuestionsCount = 0;

            foreach (var lvl in levels)
            {
                if (lvl.MarkPerQuestion > 0)
                    assignedMarksFromLevels += lvl.MarkPerQuestion * lvl.TotalCount;
                else
                    unassignedQuestionsCount += (int)lvl.TotalCount;
            }

            var remainingMarks = totalMarks - assignedMarksFromLevels;
            if (remainingMarks < 0) remainingMarks = 0;

            double perQuestionForUnassigned = unassignedQuestionsCount > 0
                ? Math.Round(remainingMarks / unassignedQuestionsCount, 3)
                : 0;

            for (int i = 0; i < difficultyLevels.Count; i++)
            {
                if (difficultyLevels[i].SelectedCount == 0)
                {
                    difficultyLevels[i].MarkPerQuestion = 0;
                }
                else if (difficultyLevels[i].MarkPerQuestion <= 0)
                {
                    difficultyLevels[i].MarkPerQuestion = perQuestionForUnassigned;
                }
            }

            foreach (var q in usedQuestion)
            {
                var lvl = difficultyLevels.FirstOrDefault(d => d.DifficultyLevelId == q.DifficultyLevelId);

                if (lvl?.SelectedCount > 0)
                {
                    var units = q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1;
                    q.Score = Math.Round(lvl.MarkPerQuestion * units, 3);
                }
                else
                {
                    q.Score = 0;
                }
            }

            var sumAfter = usedQuestion.Sum(q => q.Score ?? 0);

            var diff = Math.Round(totalMarks - sumAfter, 3);

            if (Math.Abs(diff) > 0 && usedQuestion.Count > 0)
            {
                usedQuestion[0].Score = Math.Round((usedQuestion[0].Score ?? 0) + diff, 3);
            }
        }

        private void RedistributeForItemBanks()
        {
            var totalMarks = RelatedPaper.TotalMarks;

            var groupedBanks = usedQuestion
                .GroupBy(q => q.ItembankId)
                .Select(g => new
                {
                    ItemBankId = g.Key,
                    TotalUnits = g.Sum(x => x.SubQuestionsCount > 0 ? x.SubQuestionsCount : 1)
                })
                .ToList();

            if (groupedBanks.Count == 0)
                return;

            foreach (var bank in itemBanks)
            {
                var group = groupedBanks.FirstOrDefault(g => g.ItemBankId == bank.ItemBankId);

                if (group == null || group.TotalUnits == 0)
                {
                    bank.QuestionMark = 0;
                    continue;
                }

                bank.QuestionMark = Math.Round(bank.ItemBankMark / group.TotalUnits, 3);
            }

            foreach (var q in usedQuestion)
            {
                var bank = itemBanks.FirstOrDefault(b => b.ItemBankId == q.ItembankId);

                if (bank == null || bank.QuestionMark <= 0)
                {
                    q.Score = 0;
                    continue;
                }

                var units = q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1;

                q.Score = Math.Round(bank.QuestionMark * units, 3);
            }


            var sumAfter = usedQuestion.Sum(q => q.Score ?? 0);
            var diff = Math.Round(totalMarks - sumAfter, 3);

            if (Math.Abs(diff) > 0 && usedQuestion.Count > 0)
            {
                var totalUnits = usedQuestion.Sum(q => q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1);

                if (totalUnits > 0)
                {
                    var diffPerUnit = Math.Round(diff / totalUnits, 3);

                    foreach (var q in usedQuestion)
                    {
                        var units = q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1;
                        q.Score = Math.Round((q.Score ?? 0) + (diffPerUnit * units), 3);
                    }
                }
            }
        }

        #endregion Marking Scheme Part


        #region Questions Management Part

        private async Task<CustomTableData<FormQuestionDetailedDto>> GetAllQuestionsPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            if (PaperId == 0)
            {
                return new CustomTableData<FormQuestionDetailedDto>([], 0);
            }

            var response = await BlazQuestionService.GetAllQuestionFormItemBankPoint(PaperId, paginationSearchModel);

            var sourceList = response.Items?.ToList() ?? [];

            var mappedQuestions = sourceList.ConvertAll(q => new FormQuestionDetailedDto
            {
                QuestionId = q.QuestionMetadataId,
                Code = q.Code,
                DifficultyLevelId = q.DifficultyLevelId,
                DifficultyLevelName = q.DifficultyLevelName,
                QuestionType = q.QuestionType,
                SubQuestionsCount = q.SubQuestionsCount,
                ItembankId = q.ItemBankId,
                ItemBankName = q.ItemBankName,
                PaperQuestionStatus = q.PaperQuestionStatus,
                UsedInFormsCount = q.UsedInFormsCount
            });

            return new CustomTableData<FormQuestionDetailedDto>(mappedQuestions, response.TotalItems);
        }

        private void ResetFilters()
        {
            if (SelectedItemBank is not null || SelectedType is not null || SelectedDifficultyLevel is not null)
            {
                SelectedItemBank = null;
                SelectedType = null;
                SelectedDifficultyLevel = null;

                _questionsListChangingKey--;
            }
        }

        private void SelectedQuestionsChanged(FormQuestionDetailedDto receivedSelectedQuestion)
        {
            if (receivedSelectedQuestion == null) return;

            bool isAlreadySelected = _selectedQuestions
                .Select(x => x.QuestionId)
                .Contains(receivedSelectedQuestion.QuestionId);

            if (isAlreadySelected) return;

            if (_selectedQuestions.Select(x => x.QuestionId).Contains(receivedSelectedQuestion.QuestionId))
            {
                Snackbar.Add(Resource.ThisQuestionIsAlreadySelected, Severity.Info, x =>
                {
                    x.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
                    x.VisibleStateDuration = 1500;
                    x.ShowTransitionDuration = 100;
                    x.HideTransitionDuration = 100;
                });

                StateHasChanged();

                return;
            }

            _selectedQuestions.Add(receivedSelectedQuestion);

            receivedSelectedQuestion.UsedInFormsCount += 1;

            if (_pendingHangMapping)
            {
                receivedSelectedQuestion.Score = 0;
                usedQuestion.Add(receivedSelectedQuestion);
                _pendingReplacementQuestions.Add(receivedSelectedQuestion);

                _undoReplacementMap[receivedSelectedQuestion.QuestionId] = (_lastHangedQuestionId, _pendingHangOriginalScore);

                bool isCountComplete = TotalUsedSelectedCount == RelatedPaper.QuestionsCount;

                if (isCountComplete)
                {
                    var replacementCount = _pendingReplacementQuestions.Count;
                    var scorePerReplacement = Math.Round(_pendingHangOriginalScore / replacementCount, 3);
                    var distributed = 0.0;

                    for (int i = 0; i < _pendingReplacementQuestions.Count; i++)
                    {
                        var replacement = _pendingReplacementQuestions[i];

                        if (i == _pendingReplacementQuestions.Count - 1)
                        {
                            replacement.Score = Math.Round(_pendingHangOriginalScore - distributed, 3);
                        }
                        else
                        {
                            replacement.Score = scorePerReplacement;
                            distributed += scorePerReplacement;
                        }

                        var existingLevel = difficultyLevels.FirstOrDefault(d => d.DifficultyLevelId == replacement.DifficultyLevelId);
                        if (existingLevel != null)
                        {
                            existingLevel.SelectedCount += 1;
                            existingLevel.SubQuestionCount += replacement.SubQuestionsCount;
                            existingLevel.TotalCount += replacement.SubQuestionsCount;
                            existingLevel.MarkPerQuestion = Math.Round(
                                usedQuestion
                                    .Where(q => q.DifficultyLevelId == replacement.DifficultyLevelId)
                                    .Average(q => q.Score / (q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1) ?? 0), 3);
                        }
                        else
                        {
                            difficultyLevels.Add(new GetDifficultyLevelMarkingSchemeResponseDto
                            {
                                DifficultyLevelId = replacement.DifficultyLevelId,
                                DifficultyLevelName = replacement.DifficultyLevelName,
                                SelectedCount = 1,
                                SubQuestionCount = replacement.SubQuestionsCount,
                                TotalCount = replacement.SubQuestionsCount,
                                MarkPerQuestion = Math.Round(
                                    replacement.Score / (replacement.SubQuestionsCount > 0 ? replacement.SubQuestionsCount : 1) ?? 0, 3)
                            });
                        }

                        var existingBank = itemBanks.FirstOrDefault(b => b.ItemBankId == replacement.ItembankId);
                        if (existingBank != null)
                        {
                            existingBank.QuestionCount += replacement.SubQuestionsCount;
                            existingBank.ItemBankMark = Math.Round(existingBank.ItemBankMark + replacement.Score ?? 0, 3);
                            existingBank.QuestionMark = existingBank.QuestionCount > 0
                                ? Math.Round(existingBank.ItemBankMark / existingBank.QuestionCount, 3)
                                : 0;
                        }
                        else
                        {
                            itemBanks.Add(new GetItemBanksMarkingSchemeResponseDto
                            {
                                ItemBankId = replacement.ItembankId,
                                ItemBankName = replacement.ItemBankName,
                                QuestionCount = replacement.SubQuestionsCount,
                                ItemBankMark = replacement.Score ?? 0,
                                QuestionMark = Math.Round(
                                    (replacement.Score ?? 0) / (replacement.SubQuestionsCount > 0 ? replacement.SubQuestionsCount : 1), 3)
                            });
                        }
                    }

                    _pendingReplacementQuestions.Clear();
                    _pendingHangOriginalScore = 0;
                    _pendingHangMapping = false;
                    _lastHangedQuestionId = 0;

                    RecalculateAndValidateMarksAutoOrManual();
                }
            }
            else
            {
                usedQuestion.Add(receivedSelectedQuestion);
                RedistributeScoresIfNeeded();
            }

            Snackbar.Add(Resource.QuestionSelectedSuccessfully, Severity.Success, x =>
            {
                x.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
                x.VisibleStateDuration = 1500;
                x.ShowTransitionDuration = 100;
                x.HideTransitionDuration = 100;
            });

            StateHasChanged();
        }

        private void SelectedItemsChanged(HashSet<FormQuestionDetailedDto> items)
        {
            bool addedAny = false;

            foreach (var item in items)
            {
                if (!_selectedQuestions.Select(x => x.QuestionId).Contains(item.QuestionId))
                {
                    _selectedQuestions.Add(item);

                    usedQuestion.Add(item);

                    addedAny = true;
                }
            }

            if (addedAny)
            {
                Snackbar.Add(Resource.QuestionSelectedSuccessfully, Severity.Success);

                StateHasChanged();
            }
        }

        private static string GetStatusIcon(PaperQuestionStatus status) => status switch
        {
            PaperQuestionStatus.Used => Icons.Material.Filled.CheckCircle,
            PaperQuestionStatus.Hanged => Icons.Material.Filled.Cancel,
            _ => Icons.Material.Filled.Help
        };

        private static Color GetStatusColor(PaperQuestionStatus status) => status switch
        {
            PaperQuestionStatus.Used => Color.Success,
            PaperQuestionStatus.Hanged => Color.Warning,
            _ => Color.Default
        };

        private static string GetStatusDisplayName(PaperQuestionStatus status)
        {
            return status.ToLocalizedString();
        }

        private void RemoveQuestion(FormQuestionDetailedDto question)
        {
            question.UsedInFormsCount = Math.Max(0, question.UsedInFormsCount - 1);

            if (_undoReplacementMap.TryGetValue(question.QuestionId, out var undoEntry))
            {
                var originalHanged = _selectedQuestions.FirstOrDefault(q => q.QuestionId == undoEntry.OriginalHangedId);

                if (originalHanged != null)
                {
                    originalHanged.PaperQuestionStatus = PaperQuestionStatus.Used;
                    originalHanged.Score = undoEntry.OriginalScore;

                    if (!usedQuestion.Any(u => u.QuestionId == originalHanged.QuestionId))
                        usedQuestion.Add(originalHanged);
                }

                _undoReplacementMap.Remove(question.QuestionId);
            }

            _selectedQuestions.Remove(question);
            usedQuestion.RemoveAll(u => u.QuestionId == question.QuestionId);

            RecalculateAndValidateMarksAutoOrManual();
            StateHasChanged();
        }

        private void ClearAllQuestions()
        {
            _selectedQuestions.Clear();
            usedQuestion.Clear();
            RecalculateAndValidateMarksAutoOrManual();
            StateHasChanged();
        }

        private void SyncUsedQuestionsWithAllQuestions()
        {
            usedQuestion.RemoveAll(u => !_selectedQuestions.Any(s => s.QuestionId == u.QuestionId));

            foreach (var selected in _selectedQuestions)
            {
                var existing = usedQuestion.FirstOrDefault(u => u.QuestionId == selected.QuestionId);
                if (existing == null)
                {
                    usedQuestion.Add(new FormQuestionDetailedDto
                    {
                        QuestionId = selected.QuestionId,
                        Code = selected.Code,
                        Score = 0
                    });
                }
            }

            RedistributeScoresIfNeeded();
            RecalculateAndValidateMarksAutoOrManual();
            StateHasChanged();
        }

        private void OnQuestionStatusChanged(FormQuestionDetailedDto question, PaperQuestionStatus newStatus)
        {
            question.PaperQuestionStatus = newStatus;

            if (newStatus == PaperQuestionStatus.Hanged)
            {
                _pendingHangOriginalScore = question.Score ?? 0;
                _lastHangedQuestionId = question.QuestionId;
                _undoReplacementMap[question.QuestionId] = (question.QuestionId, _pendingHangOriginalScore);

                usedQuestion.RemoveAll(u => u.QuestionId == question.QuestionId);

                var hangingLevel = difficultyLevels.FirstOrDefault(d => d.DifficultyLevelId == question.DifficultyLevelId);
                if (hangingLevel != null)
                {
                    hangingLevel.SelectedCount = Math.Max(0, hangingLevel.SelectedCount - 1);
                    hangingLevel.SubQuestionCount = Math.Max(0, hangingLevel.SubQuestionCount - question.SubQuestionsCount);
                    hangingLevel.TotalCount = Math.Max(0, hangingLevel.TotalCount - question.SubQuestionsCount);

                    if (hangingLevel.SelectedCount == 0)
                        difficultyLevels.Remove(hangingLevel);
                }

                var hangingBank = itemBanks.FirstOrDefault(b => b.ItemBankId == question.ItembankId);
                if (hangingBank != null)
                {
                    hangingBank.QuestionCount = Math.Max(0, hangingBank.QuestionCount - question.SubQuestionsCount);
                    hangingBank.ItemBankMark = Math.Max(0, Math.Round(hangingBank.ItemBankMark - (_pendingHangOriginalScore), 3));
                    hangingBank.QuestionMark = hangingBank.QuestionCount > 0
                        ? Math.Round(hangingBank.ItemBankMark / hangingBank.QuestionCount, 3)
                        : 0;

                    if (hangingBank.QuestionCount == 0)
                        itemBanks.Remove(hangingBank);
                }

                _pendingReplacementQuestions.Clear();
                _pendingHangMapping = true;
            }
            else if (newStatus == PaperQuestionStatus.Used)
            {
                if (!usedQuestion.Any(u => u.QuestionId == question.QuestionId))
                    usedQuestion.Add(question);

                if (question.Score is null or 0)
                    RedistributeScoresIfNeeded();

                _pendingHangMapping = false;
            }

            RecalculateAndValidateMarksAutoOrManual();
            StateHasChanged();
        }

        private bool IsQuestionStatusSelectionDisabled(FormQuestionDetailedDto question)
        {
            if (_pendingHangMapping)
            {
                return question.QuestionId != _lastHangedQuestionId;
            }

            return question.PaperQuestionStatus == PaperQuestionStatus.Hanged;
        }

        private async Task ViewQuestionFormsAsync(FormQuestionDetailedDto q)
        {
            var parameters = new DialogParameters<ViewQuestionWithFormDialog>
            {
                { x => x.PaperId, PaperId },
                { x => x.QuestionId, q.QuestionId }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = false
            };

            await DialogService.ShowAsync<ViewQuestionWithFormDialog>(
                string.Empty,
                parameters,
                options
            );
        }

        #endregion Questions Management Part


        #region Actions

        public async Task OnValidSubmitAsync()
        {
            var expectedQuestionCount = RelatedPaper.QuestionsCount;
            var actualQuestionCount = usedQuestion.Sum(q => q.SubQuestionsCount);
            if (actualQuestionCount != expectedQuestionCount)
            {
                Snackbar.Add($"{Resource.TheNumberOfSelectedQuestion} {actualQuestionCount} {Resource.DoesnotMatchExpected} {expectedQuestionCount} {Resource.Question}", Severity.Warning);
                return;
            }

            if (usedQuestion.Any(q => q.Score <= 0))
            {
                Snackbar.Add(Resource.EachQuestionMustHaveAScoreGreaterThanZero, Severity.Warning);
                return;
            }

            var totalScore = usedQuestion.Sum(q => q.Score);
            var totalExamMark = RelatedPaper.TotalMarks;
            if (totalScore != totalExamMark)
            {
                Snackbar.Add($"{Resource.TheTotalScoreOfTheQuestions} {totalScore} {Resource.DoesnotEqualTheTotalExamScore} {totalExamMark}", Severity.Warning);
                return;
            }

            model.FormId = FormId;
            var usedDict = usedQuestion.ToDictionary(u => u.QuestionId, u => u.Score);
            model.Questions = [.. _selectedQuestions.Select(selected => new FormQuestionEditDto
            {
                QuestionId = selected.QuestionId,
                FormQuestionStatus = selected.PaperQuestionStatus,
                Score = (double)(usedDict.TryGetValue(selected.QuestionId, out var score) ? score : 0)
            })];

            var editResponse = await BlazFormService.EditFormAsync(model);

            StateHasChanged();

            Snackbar.Add(editResponse.Message, editResponse.StatusCode == HttpStatusCode.OK ? Severity.Success : Severity.Error);

            if (editResponse.StatusCode == HttpStatusCode.OK)
                Close();
        }

        private void Close() => MudDialog.Close();

        private void OnTableSelectionChanged(HashSet<FormQuestionDetailedDto> _)
        {
            StateHasChanged();
        }

        #endregion Actions

        #region Helper properties

        private int TotalUsedSelectedCount => _selectedQuestions.Where(x => x.PaperQuestionStatus == PaperQuestionStatus.Used).Sum(x => x.SubQuestionsCount);

        private bool IsTotalLimitReached
            => TotalUsedSelectedCount >= RelatedPaper.QuestionsCount;

        private bool IsQuestionAlreadySelected(long questionId)
            => _selectedQuestions.Select(x => x.QuestionId).Contains(questionId);

        private bool IsSelectionDisabled(FormQuestionDetailedDto item)
            => IsTotalLimitReached || IsQuestionAlreadySelected(item.QuestionId);

        private string GetSelectionTooltip(FormQuestionDetailedDto item)
        {
            if (IsTotalLimitReached) return Resource.TotalQuestionsLimitReached;
            if (IsQuestionAlreadySelected(item.QuestionId)) return Resource.ThisQuestionIsAlreadySelected;
            return Resource.SelectQuestion;
        }

        #endregion
    }
}