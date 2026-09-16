using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Helper.Dtos.HotSpotDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.QuestionLayout.HotSpot
{
    public partial class HotSpotWithClicksLayout : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private int ColumnsCount { get; set; }
        private string CleanBody { get; set; } = string.Empty;

        private HotSpotQuestionDetailsDto? _hotSpotData;
        private ElementReference _canvasRef;
        private readonly string _canvasId = $"hotspotCanvas_{Guid.NewGuid():N}";
        private bool _needsCanvasInit = false;

        private static readonly Regex[] UnwantedTagPatterns =
        [
            new Regex(HotSpotBodyParsingConstants.HotspotImageContainerPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(HotSpotBodyParsingConstants.DragDropItemsSectionPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)
        ];

        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Horizontal)
                ColumnsCount = 6;
            else if (Orientation == LayoutOrientation.Vertical)
                ColumnsCount = 12;

            if (!string.IsNullOrEmpty(Model.ModelAnswer))
            {
                _hotSpotData = JsonSerializer.Deserialize<HotSpotQuestionDetailsDto>(
                    Model.ModelAnswer,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }

            CleanBody = Model.Body ?? string.Empty;

            foreach (var pattern in UnwantedTagPatterns)
            {
                var match = pattern.Match(CleanBody);
                if (match.Success)
                    CleanBody = CleanBody.Substring(0, match.Index).Trim();
            }

            if (_hotSpotData != null && !string.IsNullOrEmpty(_hotSpotData.ImageUrl))
                _needsCanvasInit = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_needsCanvasInit)
            {
                _needsCanvasInit = false;
                await InitializeAndDrawCanvasAsync();
            }
        }

        private async Task InitializeAndDrawCanvasAsync()
        {
            if (_hotSpotData == null || string.IsNullOrEmpty(_hotSpotData.ImageUrl)) return;

            await JSRuntime.InvokeAsync<JsonElement>("hotspotCanvas.initializeCanvas", _canvasId, _hotSpotData.ImageUrl);

            foreach (var area in _hotSpotData.HotSpotAreas)
            {
                if (string.IsNullOrEmpty(area.CoordinatesJson)) continue;

                var coords = JsonSerializer.Deserialize<RectangleCoordinates>(area.CoordinatesJson);
                if (coords == null) continue;

                var isCorrect = ShowCorrectAnswer && area.IsCorrectAnswer;

                await JSRuntime.InvokeVoidAsync("hotspotCanvas.drawRectangle", _canvasId, coords.X, coords.Y, coords.Width, coords.Height, isCorrect, true);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("hotspotCanvas.dispose", _canvasId);
            }
            catch { }
        }
    }
}