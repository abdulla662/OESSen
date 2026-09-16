using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.WebSearchQuestionDtos;
using OES.Helper.Enums;
using System.Text.Json;

namespace OES.Blazor.Pages.QuestionLayout.WebSearchLayout
{
    public partial class WebSearchLayout : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; } = default!;
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        public WebSearchPropertiesDto WebSearchPropertiesDto { get; set; } = new();
        private int ColumnsCount { get; set; }

        private string _keyword = string.Empty;
        private readonly int _minResults = 5;
        private List<WebSearchResultDto> _results = [];
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Vertical)
                ColumnsCount = 12;
            else
                ColumnsCount = 12;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            WebSearchPropertiesDto = JsonSerializer.Deserialize<WebSearchPropertiesDto>(
                Model.ModelAnswer,
                _jsonSerializerOptions
            );

            _keyword = ShowCorrectAnswer
                ? WebSearchPropertiesDto.Keywords.FirstOrDefault() ?? string.Empty
                : string.Empty;

            var seenUrls = new HashSet<string>(
                WebSearchPropertiesDto.Results.Select(r => r.Url),
                StringComparer.OrdinalIgnoreCase
            );

            _results = [.. WebSearchPropertiesDto.Results];

            bool isArabicSearch = Model.LanguageId == 1;

            var dummyPool = WebSearchDummyData.GetDummyPool(isArabicSearch);

            if (_results.Count < _minResults)
            {
                var neededCount = _minResults - _results.Count;

                var dummyResults = dummyPool
                    .Where(r => !seenUrls.Contains(r.Url))
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(neededCount);

                _results.AddRange(dummyResults);
            }
        }
    }
}