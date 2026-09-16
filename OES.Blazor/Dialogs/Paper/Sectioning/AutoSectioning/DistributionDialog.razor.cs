using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Dialogs.Paper.Sectioning.AutoSectioning
{
    public partial class DistributionDialog
    {
        [Inject] IBlazQuestionService QuestionService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public MixedSelectedQuestionsNodeDto SelectedItem { get; set; } = default!;
        [Parameter] public Dictionary<string, long> DifficultyBreakdown { get; set; } = default!;
        [Parameter] public bool IsUpdateMode { get; set; } = false;
        [Parameter] public Dictionary<string, long> ExistingAutoDistributions { get; set; } = [];
        [Parameter] public Dictionary<string, List<ComprehensionDistributionItem>> ExistingSubQuestionDistributions { get; set; } = [];

        public MixedSelectedQuestionsNodeDto MixedSelectedQuestions { get; set; } = new();
        private Dictionary<string, long?> DifficultyCounts { get; set; } = [];
        private Dictionary<string, long> MaxDifficultyCounts { get; set; } = [];
        private List<string> DifficultyLevels { get; set; } = [];
        private bool IsComprehension => SelectedItem?.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension;

        private long _totalSelected = 0;
        private long _totalAvailable = 0;
        private readonly HashSet<long> _expandedQuestionIds = [];
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private Dictionary<string, List<ComprehensionDistributionRow>> _subQuestionRows = [];
        private Dictionary<string, int?> _draftSubQCount = [];
        private Dictionary<string, long?> _draftPassageCount = [];

        protected override async Task OnInitializedAsync()
        {
            if (SelectedItem == null) return;

            var response = await QuestionService.GetQuestionsByItemBankAndType(SelectedItem);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                MixedSelectedQuestions = JsonSerializer.Deserialize<MixedSelectedQuestionsNodeDto>(response.Data.ToString(), _jsonSerializerOptions) ?? new();

                if (DifficultyBreakdown != null && DifficultyBreakdown.Count != 0)
                {
                    MixedSelectedQuestions.DifficultyLevelsBreakdown = DifficultyBreakdown;
                }
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            StateHasChanged();

            if (MixedSelectedQuestions.DifficultyLevelsBreakdown != null)
            {
                DifficultyLevels = [.. MixedSelectedQuestions.DifficultyLevelsBreakdown.Keys.Where(k => MixedSelectedQuestions.DifficultyLevelsBreakdown[k] > 0)];

                foreach (var difficulty in DifficultyLevels)
                {
                    if (IsUpdateMode && ExistingAutoDistributions.TryGetValue(difficulty, out var existingCount))
                    {
                        DifficultyCounts[difficulty] = existingCount;
                    }
                    else
                    {
                        DifficultyCounts[difficulty] = 0;
                    }

                    MaxDifficultyCounts[difficulty] = MixedSelectedQuestions.DifficultyLevelsBreakdown.TryGetValue(difficulty, out var value) ? value : 0;

                    if (IsComprehension)
                    {
                        if (IsUpdateMode && ExistingSubQuestionDistributions.TryGetValue(difficulty, out var existingRows))
                        {
                            _subQuestionRows[difficulty] = existingRows
                                .ConvertAll(x => new ComprehensionDistributionRow
                                {
                                    SubQuestionsCount = x.SubQuestionsCount,
                                    Count = x.Count,
                                    Available = MixedSelectedQuestions?.TotalManualQuestions?
                                        .Count(q => q.DifficultyLevelName == difficulty &&
                                                    q.SubQuestionsCount == x.SubQuestionsCount) ?? 0,
                                    NoPassagesFound = false
                                });
                        }
                        else
                        {

                            _subQuestionRows[difficulty] = [];
                        }

                        _draftSubQCount[difficulty] = null;
                        _draftPassageCount[difficulty] = null;
                    }
                }
            }

            if (IsUpdateMode && MixedSelectedQuestions.TotalManualQuestions != null && SelectedItem.TotalManualQuestions != null)
            {
                var previouslySelectedIds = SelectedItem.TotalManualQuestions.Select(q => q.Id).ToHashSet();

                foreach (var question in MixedSelectedQuestions.TotalManualQuestions)
                {
                    question.IsSelected = previouslySelectedIds.Contains(question.Id);
                }
            }

            if (IsUpdateMode)
            {
                UpdateTotalSelected();
            }
        }

        private void OnQuestionSelectionChanged(QuestionSimpleDataDto question, bool isSelected)
        {
            if (question != null)
            {
                question.IsSelected = isSelected;
            }

            StateHasChanged();
        }

        private void UpdateTotalSelected()
        {
            ResetValidation();

            foreach (var difficulty in DifficultyLevels)
            {
                if (!DifficultyCounts.TryGetValue(difficulty, out var val) || val == 0)
                {
                    _draftSubQCount[difficulty] = null;
                    _draftPassageCount[difficulty] = null;

                    if (_subQuestionRows.ContainsKey(difficulty))
                    {
                        _subQuestionRows[difficulty].Clear();
                    }
                }
            }

            // Auto-selection handling:
            var autoSelected = DifficultyCounts
                .Where(kvp => (kvp.Value ?? 0) > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? 0);

            foreach (var (difficulty, count) in autoSelected)
            {
                if (count > MaxDifficultyCounts[difficulty])
                {
                    SetError($"{difficulty} {Resource.countexceedsavailablequestions}");
                    return;
                }

                if (IsComprehension)
                {
                    if (!_subQuestionRows.TryGetValue(difficulty, out var rows) || rows.Count == 0)
                    {
                        SetError($"{difficulty}: {Resource.SubQuestionSumMustEqualPassageCount} (0/{count})");
                        return;
                    }

                    foreach (var row in rows)
                    {
                        if (row.NoPassagesFound)
                        {
                            SetError($"{difficulty}: {Resource.NoPassagesAvailable}");
                            return;
                        }

                        if ((row.Count ?? 0) > row.Available)
                        {
                            SetError($"{difficulty}: {Resource.countexceedsavailablequestions}");
                            return;
                        }
                    }

                    var rowSum = rows.Sum(r => r.Count ?? 0);
                    if (rowSum != count)
                    {
                        SetError($"{difficulty}: {Resource.SubQuestionSumMustEqualPassageCount} ({rowSum}/{count})");
                        return;
                    }
                }
            }

            // Manual-selection handling:
            var manualSelected = MixedSelectedQuestions?
                .TotalManualQuestions?
                .Where(q => q.IsSelected)
                .GroupBy(q => q.DifficultyLevelName ?? Resource.UnKnown)
                .ToDictionary(g => g.Key, g => g.Count()) ?? [];

            _totalAvailable = MixedSelectedQuestions?.CurrentQuestionsCount ?? 0;

            _totalSelected = autoSelected.Values.Sum() + manualSelected.Values.Sum();

            if (_totalSelected > _totalAvailable)
            {
                SetError(Resource.Thetotalnumberofselectedquestionsexceedstheavailablequestions);
                return;
            }
        }

        private void ResetValidation()
        {
            _totalSelected = 0;
            _hasError = false;
            _errorMessage = string.Empty;
        }

        private void SetError(string message)
        {
            _hasError = true;
            _errorMessage = message;
        }

        private void SaveDistribution()
        {
            UpdateTotalSelected();

            if (_hasError)
            {
                Snackbar.Add(_errorMessage, Severity.Error);
                return;
            }

            // Auto-distribution handling:
            var result = new DistributionResult
            {
                DifficultySelections = DifficultyLevels
                    .Where(d => (DifficultyCounts[d] ?? 0) > 0)
                    .ToDictionary(d => d, d => DifficultyCounts[d] ?? 0)
            };

            if (IsComprehension)
            {
                var filtered = _subQuestionRows
                    .Where(kvp => kvp.Value.Any(r => (r.Count ?? 0) > 0))
                    .ToList();

                var resultDict = new Dictionary<string, List<ComprehensionDistributionItem>>();

                foreach (var kvp in filtered)
                {
                    var list = kvp.Value
                        .Where(r => (r.Count ?? 0) > 0 && r.SubQuestionsCount is > 0)
                        .Select(r => new ComprehensionDistributionItem(
                            r.SubQuestionsCount!.Value,
                            r.Count!.Value
                        ))
                        .ToList();

                    resultDict[kvp.Key] = list;
                }

                result.SubQuestionDistributions = resultDict;
            }

            // Manual-distribution handling:
            var manualSelected = MixedSelectedQuestions?
                .TotalManualQuestions?
                .Where(q => q.IsSelected)
                .ToList();

            if (manualSelected?.Count > 0)
            {
                result.SelectedQuestions = [.. manualSelected
                    .Select(q => new QuestionSimpleDataDto
                    {
                        Id = q.Id,
                        Code = q.Code,
                        Body = q.Body,
                        IsSelected = q.IsSelected,
                        DifficultyLevelName = q.DifficultyLevelName,
                        DifficultyLevelId = q.DifficultyLevelId,
                        SubQuestionsCount = q.SubQuestionsCount,
                    })
                ];
            }

            if (result.TotalSelected == 0)
            {
                Snackbar.Add(Resource.Noquestionsselected, Severity.Error);
                return;
            }

            MudDialog.Close(DialogResult.Ok(result));
        }

        private void CancelDistribution()
        {
            MudDialog.Cancel();
        }

        private static string GetDifficultyColor(string difficulty)
        {
            return difficulty switch
            {
                "Hard" => "#ff5252",
                "Medium" => "#ff9800",
                "Easy" => "#4caf50",
                "Low" => "#4caf50",
                _ => "#0288d1"
            };
        }

        private static string GetDifficultyIcon(string difficulty)
        {
            return difficulty switch
            {
                "Hard" => Icons.Material.Outlined.TrendingUp,
                "Medium" => Icons.Material.Outlined.LinearScale,
                "Easy" => Icons.Material.Outlined.TrendingDown,
                "Low" => Icons.Material.Outlined.TrendingDown,
                _ => Icons.Material.Outlined.Help
            };
        }

        private static string CleanHtmlLineBreaks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            html = Regex.Replace(html, @"<p>(\s|&nbsp;|<br\s*/?>)*</p>", string.Empty, RegexOptions.IgnoreCase);

            return html.Trim();
        }

        private void ToggleQuestionBodyExpand(long questionId)
        {
            if (!_expandedQuestionIds.Remove(questionId))
                _expandedQuestionIds.Add(questionId);

            StateHasChanged();
        }

        private void OnDraftSubQuestionChanged(string difficulty, string? rawValue)
        {
            _draftSubQCount[difficulty] = int.TryParse(rawValue, out var v) && v > 0 ? v : null;
            StateHasChanged();
        }

        private void OnDraftPassageCountChanged(string difficulty, string? rawValue)
        {
            _draftPassageCount[difficulty] = long.TryParse(rawValue, out var v) && v > 0 ? v : null;
            StateHasChanged();
        }

        private void AddSubQuestionRow(string difficulty)
        {
            _draftSubQCount.TryGetValue(difficulty, out var subQuestion);
            _draftPassageCount.TryGetValue(difficulty, out var count);

            if (subQuestion is null or <= 0 || count is null or <= 0)
            {
                Snackbar.Add(Resource.PleaseEnterValidValues, Severity.Warning);
                return;
            }

            if (_subQuestionRows.TryGetValue(difficulty, out var existingRows) &&
                existingRows.Any(r => r.SubQuestionsCount == subQuestion))
            {
                Snackbar.Add(Resource.DuplicateSubQuestionCount, Severity.Warning);
                return;
            }

            var available = MixedSelectedQuestions?.TotalManualQuestions?
                .Count(q => q.DifficultyLevelName == difficulty && q.SubQuestionsCount == subQuestion) ?? 0;

            if (available == 0)
            {
                Snackbar.Add(Resource.NoPassagesAvailable, Severity.Error);
                return;
            }

            if (count > available)
            {
                Snackbar.Add($"{difficulty}: {Resource.countexceedsavailablequestions}", Severity.Error);
                return;
            }

            if (!_subQuestionRows.ContainsKey(difficulty))
            {
                _subQuestionRows[difficulty] = [];
            }

            _subQuestionRows[difficulty].Add(new ComprehensionDistributionRow
            {
                SubQuestionsCount = subQuestion,
                Count = count,
                Available = available,
                NoPassagesFound = false
            });

            _draftSubQCount[difficulty] = null;
            _draftPassageCount[difficulty] = null;

            UpdateTotalSelected();
            StateHasChanged();
        }

        private void RemoveSubQuestionRow(string difficulty, ComprehensionDistributionRow row)
        {
            if (_subQuestionRows.TryGetValue(difficulty, out var rows))
            {
                rows.Remove(row);
            }

            UpdateTotalSelected();
            StateHasChanged();
        }

        private int GetDraftAvailable(string difficulty)
        {
            if (!_draftSubQCount.TryGetValue(difficulty, out var subQuestion) || subQuestion is null or <= 0)
            {
                return 0;
            }

            return MixedSelectedQuestions?.TotalManualQuestions?
                .Count(q => q.DifficultyLevelName == difficulty &&
                q.SubQuestionsCount == subQuestion) ?? 0;
        }

        private bool HasQuestionCount(string difficulty)
        {
            return DifficultyCounts.TryGetValue(difficulty, out var val) && val > 0;
        }
    }
}