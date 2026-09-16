using OES.Helper.Enums;

namespace OES.Helper.Dtos.Document.Response
{
    public sealed record ExtractionResult
    {
        public bool Success { get; init; }

        public IReadOnlyList<ExtractedContent> Content { get; init; } = [];

        public int PageCount { get; init; }

        public bool UsedOcr { get; init; }

        public string? Error { get; init; }

        public static ExtractionResult Failed(string error) =>
            new()
            {
                Success = false,
                Error = error
            };

        public static ExtractionResult Succeeded(IReadOnlyList<ExtractedContent> content, int pageCount = 1, bool usedOcr = false) =>
        new()
        {
            Content = content,
            Success = true,
            PageCount = pageCount,
            UsedOcr = usedOcr,
        };

        public static ExtractionResult Succeeded(string content, int pageCount = 1, bool usedOcr = false) =>
            Succeeded(
                [new()
                {
                    Type = ExtractedContentType.Text,
                    Content = content,
                    PageNumber = 1
                }],
                pageCount,
                usedOcr
            );

        public sealed record ExtractedContent
        {
            public ExtractedContentType Type { get; init; }

            public string Content { get; init; } = string.Empty;

            public int PageNumber { get; init; }

            public string? Reference { get; init; }

            public ExtractedImage? Image { get; init; }
        }

        public sealed record ExtractedImage
        {
            public string Reference { get; init; } = string.Empty;

            public string FileName { get; init; } = string.Empty;

            public string ContentType { get; init; } = string.Empty;

            public string? Description { get; init; }

            public Guid DocumentId { get; init; }

            public int Width { get; init; }

            public int Height { get; init; }
        }
    }
}
