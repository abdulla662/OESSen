using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.Enums;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.QuestionLayout.MatchingPairsWithDragDropLayout
{
    public partial class MatchingPairsWithDragDropLayout : ComponentBase
    {
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
        [Parameter] public QuestionDetailsVersionDto VersionData { get; set; }

        private GetMatchingPairsWithDragDropResponseDto MatchingData { get; set; }
        private List<MatchingPairQuestionItemDto> QuestionItems { get; set; } = [];
        private List<MatchingPairQuestionItemDto> AnswerItems { get; set; } = [];

        private Dictionary<long, List<MatchingPairQuestionItemDto>> _answersByQuestionId = [];

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // If version snapshot data is provided, use it directly
            if (VersionData?.MatchingPairQuestionItems != null && VersionData.MatchingPairQuestionItems.Count > 0)
            {
                var versionItems = VersionData.MatchingPairQuestionItems
                    .OrderBy(x => x.ColumnOrder)
                    .ThenBy(x => x.Id)
                    .ToList();

                var allItems = versionItems.ConvertAll(v => new MatchingPairQuestionItemDto
                {
                    Id = v.Id,
                    Body = v.Body,
                    ColumnOrder = v.ColumnOrder,
                    IsDataSource = v.IsDataSource
                });

                QuestionItems = [.. allItems.Where(i => i.IsDataSource)];
                AnswerItems = [.. allItems.Where(i => !i.IsDataSource)];

                // Build answers from version ModelAnswer JSON if available
                if (!string.IsNullOrWhiteSpace(VersionData.ModelAnswer))
                {
                    _answersByQuestionId = BuildAnswers(VersionData.ModelAnswer, allItems);
                }

                return;
            }

            // Otherwise, load live data from the API
            var response = await BlazQuestionService.GetMatchingPairsWithDragDropQuestionAsync(Model.QuestionMetadataId, Model.LanguageId);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Error);
                return;
            }

            MatchingData = response.Data as GetMatchingPairsWithDragDropResponseDto;

            if (MatchingData?.MatchingItems != null)
            {
                var allItems = MatchingData.MatchingItems.ToList();
                QuestionItems = [.. allItems.Where(i => i.IsDataSource)];
                AnswerItems = [.. allItems.Where(i => !i.IsDataSource)];

                _answersByQuestionId = BuildAnswers(
                    MatchingData.QuestionDetails.FirstOrDefault()?.ModelAnswer,
                    allItems
                );
            }
        }

        private Dictionary<long, List<MatchingPairQuestionItemDto>> BuildAnswers(
            string modelAnswerJson,
            List<MatchingPairQuestionItemDto> allItems
        )
        {
            var result = new Dictionary<long, List<MatchingPairQuestionItemDto>>();

            if (string.IsNullOrWhiteSpace(modelAnswerJson))
                return result;

            List<MatchingPairModelAnswerDto> mapping;
            try
            {
                mapping = JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(modelAnswerJson);
                if (mapping == null || mapping.Count == 0) return result;
            }
            catch
            {
                return result;
            }

            var itemById = allItems.ToDictionary(i => i.Id);

            // 1. Direct ID matching
            bool directMatch = false;
            foreach (var item in mapping)
            {
                var questionId = item.QuestionItemId;
                var answerIds = item.AnswerIds ?? [];

                var answers = answerIds
                    .Where(id => itemById.ContainsKey(id))
                    .Select(id => itemById[id])
                    .ToList();

                if (answers.Count > 0)
                {
                    result[questionId] = answers;
                    directMatch = true;
                }
            }

            if (directMatch)
                return result;

            // 2. Fallback for version snapshots
            var questionItems = allItems.Where(i => i.IsDataSource).OrderBy(i => i.Id).ToList();
            var answerItems = allItems.Where(i => !i.IsDataSource).OrderBy(i => i.Id).ToList();

            var sortedModelQuestionIds = mapping.Select(m => m.QuestionItemId).Distinct().OrderBy(id => id).ToList();
            var sortedModelAnswerIds = mapping.SelectMany(m => m.AnswerIds ?? []).Distinct().OrderBy(id => id).ToList();

            var questionIdByIndex = new Dictionary<long, long>();
            for (int i = 0; i < sortedModelQuestionIds.Count && i < questionItems.Count; i++)
            {
                questionIdByIndex[sortedModelQuestionIds[i]] = questionItems[i].Id;
            }

            var answerItemByIndex = new Dictionary<long, MatchingPairQuestionItemDto>();
            for (int i = 0; i < sortedModelAnswerIds.Count && i < answerItems.Count; i++)
            {
                answerItemByIndex[sortedModelAnswerIds[i]] = answerItems[i];
            }

            foreach (var item in mapping)
            {
                if (questionIdByIndex.TryGetValue(item.QuestionItemId, out var resolvedQId))
                {
                    var resolvedAnswers = (item.AnswerIds ?? [])
                        .Where(ansId => answerItemByIndex.ContainsKey(ansId))
                        .Select(ansId => answerItemByIndex[ansId])
                        .ToList();

                    if (resolvedAnswers.Count > 0)
                    {
                        result[resolvedQId] = resolvedAnswers;
                    }
                }
            }

            return result;
        }
    }
}