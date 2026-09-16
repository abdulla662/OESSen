using SharedHelper.Enums;

namespace OES.Helper.Dtos.HotSpotDtos
{
    public sealed record HotSpotQuestionDetailsDto
    {
        public int Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImageAltText { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public HotSpotQuestionType Mode { get; set; }
        public List<HotSpotAreaDto> HotSpotAreas { get; set; } = [];
        public List<DraggableItemDto> DraggableItems { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed record HotSpotAreaDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public HotSpotShapeType ShapeType { get; set; }
        public string CoordinatesJson { get; set; } = string.Empty;
        public bool IsCorrectAnswer { get; set; }
        public string? Feedback { get; set; }
    }

    public sealed record DraggableItemDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsDistractor { get; set; } = false;
        public int OrderIndex { get; set; }
        public List<Guid> AcceptedDropZoneIds { get; set; } = [];
    }

    public sealed record RectangleCoordinates
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public sealed record PointDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public sealed record HotSpotQuestionSettingsDto
    {
        public HotSpotInteractionMode Mode { get; set; }
        public HotSpotShapeType ShapeType { get; set; }
    }

    public class HotSpotZoneInfo
    {
        public Guid Id { get; set; }
        public double LeftPercent { get; set; }
        public double TopPercent { get; set; }
        public double WidthPercent { get; set; }
        public double HeightPercent { get; set; }
    }
}
