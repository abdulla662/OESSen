namespace OES.Helper.Dtos.Document.Request
{
    public sealed record DoclingConvertRequestOptions
    {
        public string[] ToFormats { get; init; } = [];

        public bool DoOcr { get; init; }

        public bool ForceOcr { get; init; }

        public string OcrEngine { get; init; } = string.Empty;

        public string[] OcrLang { get; init; } = [];

        public bool GeneratePictureImages { get; init; }

        public bool DoPictureDescription { get; init; }

        public double PictureDescriptionAreaThreshold { get; set; } = 0.05;
    }
}
