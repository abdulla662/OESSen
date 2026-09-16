using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.HotSpotDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.QuestionLayout.HotSpot
{
    public partial class HotSpotWithDragAndDropLayout : ComponentBase
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private string CleanBody { get; set; } = string.Empty;
        private HotSpotQuestionDetailsDto? QuestionData { get; set; }
        private List<DraggableItemInternal> DraggableItems { get; set; } = [];
        private List<HotSpotZoneInfo> HotSpotZones { get; set; } = [];

        private static readonly Regex[] UnwantedTagPatterns =
        [
            new Regex(HotSpotBodyParsingConstants.HotspotImageContainerPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(HotSpotBodyParsingConstants.DragDropItemsSectionPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)
        ];

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(Model.ModelAnswer))
            {
                QuestionData = JsonSerializer.Deserialize<HotSpotQuestionDetailsDto>(Model.ModelAnswer, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (QuestionData?.DraggableItems != null)
                {
                    DraggableItems = QuestionData.DraggableItems.ConvertAll(i => new DraggableItemInternal
                    {
                        Id = i.Id,
                        Label = i.Label,
                        AcceptedDropZoneIds = i.AcceptedDropZoneIds ?? [],
                        CurrentZoneId = ShowCorrectAnswer && i.AcceptedDropZoneIds != null && i.AcceptedDropZoneIds.Any()
                            ? i.AcceptedDropZoneIds.First()
                            : null
                    });
                }

                if (QuestionData?.HotSpotAreas != null && QuestionData.ImageWidth > 0 && QuestionData.ImageHeight > 0)
                {
                    HotSpotZones = QuestionData.HotSpotAreas
                        .Select(area =>
                        {
                            if (string.IsNullOrEmpty(area.CoordinatesJson)) return null;

                            var coords = JsonSerializer.Deserialize<RectangleCoordinates>(area.CoordinatesJson);
                            if (coords == null) return null;

                            return new HotSpotZoneInfo
                            {
                                Id = area.Id,
                                LeftPercent = coords.X * 100.0 / QuestionData.ImageWidth,
                                TopPercent = coords.Y * 100.0 / QuestionData.ImageHeight,
                                WidthPercent = coords.Width * 100.0 / QuestionData.ImageWidth,
                                HeightPercent = coords.Height * 100.0 / QuestionData.ImageHeight
                            };
                        })
                        .Where(z => z != null)
                        .ToList()!;
                }
            }

            CleanBody = Model.Body ?? string.Empty;

            foreach (var pattern in UnwantedTagPatterns)
            {
                var match = pattern.Match(CleanBody);
                if (match.Success)
                    CleanBody = CleanBody.Substring(0, match.Index).Trim();
            }
        }

        private void ItemUpdated(MudItemDropInfo<DraggableItemInternal> dropInfo)
        {
            if (dropInfo.DropzoneIdentifier == HotSpotBodyParsingConstants.DropzonePool)
                dropInfo.Item.CurrentZoneId = null;
            else
                dropInfo.Item.CurrentZoneId = Guid.Parse(dropInfo.DropzoneIdentifier);
        }

        private string GetWrapperStyle()
        {
            var culture = CultureInfo.InvariantCulture;
            var ratio = QuestionData!.ImageWidth > 0 && QuestionData.ImageHeight > 0
                ? $"aspect-ratio:{QuestionData.ImageWidth.ToString(culture)} / {QuestionData.ImageHeight.ToString(culture)}; "
                : string.Empty;

            return "position:relative; width:100%; border-radius:12px; overflow:hidden; " +
                   "border:1px solid #e0e0e0; " + ratio +
                   $"background-image:url('{QuestionData.ImageUrl}'); " +
                   "background-size:100% 100%; background-repeat:no-repeat; background-position:center;";
        }

        private string GetZoneWrapperStyle(HotSpotZoneInfo zone) =>
            "position:absolute; " +
            $"left:{zone.LeftPercent.ToString(CultureInfo.InvariantCulture)}%; " +
            $"top:{zone.TopPercent.ToString(CultureInfo.InvariantCulture)}%; " +
            $"width:{zone.WidthPercent.ToString(CultureInfo.InvariantCulture)}%; " +
            $"height:{zone.HeightPercent.ToString(CultureInfo.InvariantCulture)}%;";

        public class DraggableItemInternal
        {
            public Guid Id { get; set; }
            public string Label { get; set; }
            public Guid? CurrentZoneId { get; set; }
            public List<Guid> AcceptedDropZoneIds { get; set; } = [];
        }
    }
}