using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Document.Response
{
    public sealed record DoclingConvertResponse
    {
        public DoclingResponseDocument? Document { get; set; }
    }

    public sealed record DoclingResponseDocument
    {
        public DoclingDocument? JsonContent { get; set; }
    }

    public sealed record DoclingDocument
    {
        public DoclingGroupNode Body { get; set; } = new();
        public List<DoclingTextItem> Texts { get; set; } = [];
        public List<DoclingPictureItem> Pictures { get; set; } = [];
        public List<DoclingTableItem> Tables { get; set; } = [];
        public List<DoclingGroupNode> Groups { get; set; } = [];
        public Dictionary<string, object>? Pages { get; set; }
    }

    public sealed record DoclingRef
    {
        [JsonPropertyName("$ref")] public string Ref { get; set; } = string.Empty;
    }

    public sealed record DoclingGroupNode
    {
        public string SelfRef { get; set; } = string.Empty;
        public List<DoclingRef> Children { get; set; } = new();
    }

    public sealed record DoclingProvenance
    {
        public int PageNo { get; set; }
    }

    public sealed record DoclingTextItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Orig { get; set; }
        public string? Label { get; set; }
        public string? ContentLayer { get; set; }
        public List<DoclingProvenance>? Prov { get; set; }
    }

    public sealed record DoclingPictureItem
    {
        public DoclingImagePayload? Image { get; set; }
        public List<DoclingRef> Children { get; set; } = new();
        public List<DoclingProvenance>? Prov { get; set; }
        public DoclingPictureMeta? Meta { get; set; }
    }

    public sealed record DoclingPictureMeta
    {
        public DoclingPictureDescription? Description { get; set; }
    }

    public sealed record DoclingPictureDescription
    {
        public double? Confidence { get; set; }
        public string? CreatedBy { get; set; }
        public string? Text { get; set; }
    }

    public sealed record DoclingImagePayload
    {
        [JsonPropertyName("mimetype")] public string? MimeType { get; set; }
        public double? Dpi { get; set; }
        public DoclingImageSize? Size { get; set; }
        public string? Uri { get; set; }
    }

    public sealed record DoclingImageSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public sealed record DoclingTableItem
    {
        public DoclingTableData? Data { get; set; }
        public List<DoclingProvenance>? Prov { get; set; }
    }

    public sealed record DoclingTableData
    {
        public List<DoclingTableCell> TableCells { get; set; } = [];
        public int NumRows { get; set; }
        public int NumCols { get; set; }
    }

    public sealed record DoclingTableCell
    {
        public int RowSpan { get; set; }
        public int ColSpan { get; set; }
        public int StartRowOffsetIdx { get; set; }
        public int EndRowOffsetIdx { get; set; }
        public int StartColOffsetIdx { get; set; }
        public int EndColOffsetIdx { get; set; }
        public string? Text { get; set; }
        public bool ColumnHeader { get; set; }
        public bool RowHeader { get; set; }
        public bool RowSection { get; set; }
        public bool Fillable { get; set; }
    }
}