using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.Enums;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.QuestionLayout.MatchingPairsLayout
{
    public partial class MatchingPairsLayout : ComponentBase
    {
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
        [Parameter] public QuestionDetailsVersionDto VersionData { get; set; }

        private GetMatchingPairsResponseDto MatchingData { get; set; }
        private List<QuestionDetailsDto> PairItems { get; set; } = [];
        private List<MatchingPairAnswerView> MatchingPairAnswers { get; set; } = [];
        private List<MatchingPairRowView> MatchingPairRows { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            await LoadMatchingPairsAsync();
        }

        private async Task LoadMatchingPairsAsync()
        {
            if (VersionData?.MatchingPairQuestionItems != null && VersionData.MatchingPairQuestionItems.Count > 0)
            {
                PrepareVersionData();
                return;
            }

            var response = await BlazQuestionService.GetMatchingPairsQuestionAsync(Model.QuestionMetadataId, Model.LanguageId);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Error);
                return;
            }

            MatchingData = response.Data as GetMatchingPairsResponseDto;

            PrepareData();
        }

        private void PrepareVersionData()
        {
            var versionItems = VersionData.MatchingPairQuestionItems
                .OrderBy(x => x.ColumnOrder)
                .ThenBy(x => x.Id)
                .ToList();

            var questionItems = versionItems.Where(i => i.IsDataSource).ToList();
            var answerItems = versionItems.Where(i => !i.IsDataSource).ToList();

            MatchingPairAnswers = answerItems.ConvertAll(a => new MatchingPairAnswerView
            {
                Text = a.Body,
                LanguageId = Model.LanguageId
            });

            var modelAnswerMapping = ParseModelAnswerMapping(
                VersionData.ModelAnswer,
                questionItems.Select(q => (q.Id, q.Body)),
                answerItems.Select(a => (a.Id, a.Body))
            );

            MatchingPairRows = questionItems.ConvertAll(q =>
            {
                var correctAnswer = modelAnswerMapping.TryGetValue(q.Id, out var ans) ? ans : string.Empty;
                return new MatchingPairRowView
                {
                    Term = q.Body,
                    CorrectAnswer = correctAnswer,
                    SelectedAnswer = ShowCorrectAnswer ? correctAnswer : string.Empty,
                    LanguageId = Model.LanguageId
                };
            });
        }

        private void PrepareData()
        {
            if (MatchingData == null) return;

            if (MatchingData.MatchingItems != null && MatchingData.MatchingItems.Count != 0)
            {
                var questionItems = MatchingData.MatchingItems.Where(i => i.IsDataSource).OrderBy(i => i.Id).ToList();
                var answerItems = MatchingData.MatchingItems.Where(i => !i.IsDataSource).OrderBy(i => i.Id).ToList();

                MatchingPairAnswers = answerItems.ConvertAll(a => new MatchingPairAnswerView
                {
                    Text = a.Body,
                    LanguageId = Model.LanguageId
                });

                var detail = MatchingData.QuestionDetails?.FirstOrDefault(x => x.LanguageId == Model.LanguageId)
                             ?? MatchingData.QuestionDetails?.FirstOrDefault();

                var modelAnswerMapping = ParseModelAnswerMapping(
                    detail?.ModelAnswer,
                    questionItems.Select(q => (q.Id, q.Body)),
                    answerItems.Select(a => (a.Id, a.Body))
                );

                MatchingPairRows = questionItems.ConvertAll(q =>
                {
                    var correctAnswer = modelAnswerMapping.TryGetValue(q.Id, out var ans) ? ans : string.Empty;
                    return new MatchingPairRowView
                    {
                        Term = q.Body,
                        CorrectAnswer = correctAnswer,
                        SelectedAnswer = ShowCorrectAnswer ? correctAnswer : string.Empty,
                        LanguageId = Model.LanguageId
                    };
                });
            }
            else
            {
                var parentMetadataId = MatchingData.MetadataParentId;

                var langItems = MatchingData.QuestionDetails?.Where(x => x.LanguageId == Model.LanguageId).ToList() ?? [];

                PairItems = [.. langItems.Where(x => x.QuestionMetadataId != parentMetadataId)];

                MatchingPairAnswers = [.. PairItems
                    .SelectMany(q => (q.Choices ?? [])
                        .Where(c => c.IsCorrectAnswer)
                        .Select(c => new MatchingPairAnswerView
                        {
                            Text = c.ChoiceText,
                            LanguageId = q.LanguageId
                        })
                    )
                ];

                MatchingPairRows = PairItems.ConvertAll(p => new MatchingPairRowView
                {
                    Term = p.Body,
                    CorrectAnswer = p.ModelAnswer,
                    SelectedAnswer = ShowCorrectAnswer ? p.ModelAnswer : string.Empty,
                    LanguageId = p.LanguageId
                });
            }
        }

        private static Dictionary<long, string> ParseModelAnswerMapping(
            string modelAnswerJson,
            IEnumerable<(long Id, string Body)> questionItems,
            IEnumerable<(long Id, string Body)> answerItems)
        {
            var result = new Dictionary<long, string>();

            if (string.IsNullOrWhiteSpace(modelAnswerJson))
            {
                return result;
            }

            List<MatchingPairModelAnswerDto> mapping;
            try
            {
                mapping = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(modelAnswerJson);
                if (mapping == null || mapping.Count == 0)
                {
                    return result;
                }
            }
            catch
            {
                return result;
            }

            var qList = questionItems.ToList();
            var aList = answerItems.ToList();
            var answerMap = aList.ToDictionary(a => a.Id, a => a.Body);

            // 1. Direct ID matching (standard for live data or when IDs match)
            bool directMatch = false;
            foreach (var item in mapping)
            {
                if (item.AnswerIds != null && item.AnswerIds.Count > 0)
                {
                    var firstAnsId = item.AnswerIds.First();
                    if (answerMap.TryGetValue(firstAnsId, out var answerText))
                    {
                        result[item.QuestionItemId] = answerText;
                        directMatch = true;
                    }
                }
            }

            if (directMatch)
            {
                return result;
            }

            // 2. Fallback for version snapshots:
            // In version snapshots, ModelAnswer contains original MatchingPairQuestionItems entity IDs,
            // while VersionData.MatchingPairQuestionItems contains new version snapshot IDs.
            // Both original items and version items are inserted and sorted consistently by Id.
            var sortedModelQuestionIds = mapping.Select(m => m.QuestionItemId).Distinct().OrderBy(id => id).ToList();
            var sortedModelAnswerIds = mapping.SelectMany(m => m.AnswerIds ?? []).Distinct().OrderBy(id => id).ToList();

            var sortedQuestionItems = qList.OrderBy(q => q.Id).ToList();
            var sortedAnswerItems = aList.OrderBy(a => a.Id).ToList();

            var questionIdByIndex = new Dictionary<long, long>();
            for (int i = 0; i < sortedModelQuestionIds.Count && i < sortedQuestionItems.Count; i++)
            {
                questionIdByIndex[sortedModelQuestionIds[i]] = sortedQuestionItems[i].Id;
            }

            var answerTextByIndex = new Dictionary<long, string>();
            for (int i = 0; i < sortedModelAnswerIds.Count && i < sortedAnswerItems.Count; i++)
            {
                answerTextByIndex[sortedModelAnswerIds[i]] = sortedAnswerItems[i].Body;
            }

            foreach (var item in mapping)
            {
                if (questionIdByIndex.TryGetValue(item.QuestionItemId, out var resolvedQId))
                {
                    if (item.AnswerIds != null && item.AnswerIds.Count > 0)
                    {
                        var firstAnsId = item.AnswerIds.First();
                        if (answerTextByIndex.TryGetValue(firstAnsId, out var answerText))
                        {
                            result[resolvedQId] = answerText;
                        }
                    }
                }
            }

            return result;
        }
    }
}